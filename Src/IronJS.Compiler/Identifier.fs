﻿namespace IronJS.Compiler

open IronJS
open IronJS.Compiler
  
//------------------------------------------------------------------------------
module Identifier =
  
  //----------------------------------------------------------------------------
  let private walkScopeChain expr target current =
    let rec walk expr times = 
      if times = 0
        then expr
        else walk (Dlr.field (Dlr.index0 expr) "Scope") (times-1)

    walk expr (current - target)
      
  //----------------------------------------------------------------------------
  let getData (ctx:Ctx) name =
    match Seq.tryFind (Ast.hasVar name) ctx.ScopeChain with
    | Some scope -> Variable(scope, Ast.getVar name scope)
    | None -> 
      match Seq.tryFind (Ast.hasCls name) ctx.ScopeChain with
      | None -> Global
      | Some scope -> Closure(Ast.getCls name scope)
        
  //----------------------------------------------------------------------------
  let clsExprAndIndex ctx (cls:Ast.Closure) =
    let expr = 
      (walkScopeChain
        (ctx.ChainExpr)
        (cls.ClosureLevel)
        (ctx.Scope.ClosureLevel)
      )

    Some(expr, cls.Index, cls.GlobalLevel, None)
      
  //----------------------------------------------------------------------------
  let varExprAndIndex ctx (scope:Ast.Scope) (var:Ast.Variable) =
    let expr =
      if var.IsClosedOver then
        (walkScopeChain
          (ctx.ChainExpr)
          (scope.ClosureLevel)
          (ctx.Scope.ClosureLevel)
        )
      else
        (walkScopeChain
          (ctx.LocalExpr)
          (scope.LocalLevel)
          (ctx.Scope.LocalLevel)
        )

    Some(expr, var.Index, scope.GlobalLevel, var.Type)
    
  //----------------------------------------------------------------------------
  let isGlobal ctx name =
    getData ctx name = Global
    
  //----------------------------------------------------------------------------
  let getExprIndexLevelType ctx name =
    match getData ctx name with
    | Global -> None
    | Variable(scope, var) -> varExprAndIndex ctx scope var
    | Closure cls -> clsExprAndIndex ctx cls

  //----------------------------------------------------------------------------
  let dynamicGetGlobalArgs (ctx:Ctx) name = 
    [Dlr.neg1; ctx.Globals; Dlr.defaultT<Scope>; Dlr.neg1]

  //----------------------------------------------------------------------------
  let dynamicGetVariableArgs (ctx:Ctx) expr (name:string) (i:int) (l:int) =
    [Dlr.const' l; ctx.Globals; expr; Dlr.const' i]
          
  //----------------------------------------------------------------------------
  let getDynamicArgs (ctx:Ctx) name =
    match getExprIndexLevelType ctx name with
    | None -> dynamicGetGlobalArgs ctx name
    | Some(expr, i, l, _) -> dynamicGetVariableArgs ctx expr name i l
          
  //----------------------------------------------------------------------------
  let getValueDynamic (ctx:Ctx) name =
    let defaultArgs = [Dlr.const' name; ctx.DynamicExpr]
    let dynamicArgs = getDynamicArgs ctx name
    let args = defaultArgs @ dynamicArgs
    Dlr.callStaticT<Helpers.ScopeHelpers> "DynamicGet" (args)
          
  //----------------------------------------------------------------------------
  let setValueDynamic (ctx:Ctx) name value =
    let defaultArgs = [Dlr.const' name; Expr.boxValue value; ctx.DynamicExpr]
    let dynamicArgs = getDynamicArgs ctx name
    let args = defaultArgs @ dynamicArgs
    Dlr.callStaticT<Helpers.ScopeHelpers> "DynamicSet" (args)
        
  //----------------------------------------------------------------------------
  let getValue (ctx:Ctx) name =
    match ctx.DynamicLookup with
    | true -> getValueDynamic ctx name
    | _ -> 
      match ctx.Scope.ScopeType with
      | Ast.GlobalScope -> Api.Expr.jsObjectGetProperty ctx.Globals name
      | _ ->
        match getExprIndexLevelType ctx name with
        | None -> Api.Expr.jsObjectGetProperty ctx.Globals name
        | Some(expr, i, _, tc) -> Dlr.Ext.static' (Expr.unboxIndex expr i tc)
        
  //----------------------------------------------------------------------------
  let setValue (ctx:Ctx) name value =
    match ctx.DynamicLookup with
    | true -> setValueDynamic ctx name value
    | _ ->
      match getExprIndexLevelType ctx name with
      | None -> 
        Expr.blockTmp value (fun value ->
          [Api.Expr.jsObjectPutProperty ctx.Globals name value]
        )

      | Some(expr, i, _, tc) -> 
        let varExpr = Expr.unboxIndex expr i tc
        Expr.assignValue (Dlr.Ext.static' varExpr) value
