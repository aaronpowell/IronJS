﻿namespace IronJS.Compiler

open IronJS
open IronJS.Compiler

module Increment =

  //----------------------------------------------------------------------------
  type ChangeVariable = 
    Ctx -> Dlr.Expr -> TypeCode option -> Dlr.Expr
    
  //----------------------------------------------------------------------------
  type ChangeProperty = 
    Ctx -> Dlr.Expr -> string -> Dlr.Expr
    
  //----------------------------------------------------------------------------
  let postChangeVariable op (ctx:Ctx) expr tc =
    match tc with
    | None -> 
      Dlr.blockTmpT<IjsBox> (fun tmp ->
        [
          (Dlr.assign tmp expr)
          (Dlr.ternary
            (Expr.containsNumber expr)
            (Dlr.blockSimple [
              (Expr.updateBoxValue 
                (expr)
                (op (Expr.unboxNumber expr) Dlr.dbl1)
              )
              (tmp :> Dlr.Expr)
            ])
            (ctx.Env_Boxed_Zero) //TODO: Fallback for non-numbers
          )
        ] |> Seq.ofList
      )

    | Some tc ->
      match tc with
      | TypeCodes.Number ->
        Dlr.blockTmpT<IjsNum> (fun tmp -> 
          [
            (Dlr.assign tmp expr)
            (Dlr.assign expr (op expr Dlr.dbl1))
            (tmp :> Dlr.Expr)
          ] |> Seq.ofList
        )
                 
      | _ ->failwith "Que?"

  //foo++, foo--
  let postIncrementVariable : ChangeVariable = postChangeVariable Dlr.add
  let postDecrementVariable : ChangeVariable = postChangeVariable Dlr.sub
      
  //----------------------------------------------------------------------------
  let postChangeProperty op (ctx:Ctx) expr name =
    Dlr.blockTmpT<IjsBox> (fun tmp ->
      [
        (Dlr.assign 
          (tmp)
          (Api.Expr.jsObjectGetProperty expr name)
        )
        (Dlr.ternary
          (Expr.containsNumber tmp)
          (Dlr.blockSimple
            [
              (Api.Expr.jsObjectUpdateProperty 
                (expr) 
                (name)
                (op (Expr.unboxNumber tmp) Dlr.dbl1)
              )
              (tmp)
            ]
          )
          (ctx.Env_Boxed_Undefined) //TODO: Fallback for non-numbers
        )
      ] |> Seq.ofList
    )

  //foo.bar++, foo.bar--
  let postIncrementProperty : ChangeProperty = postChangeProperty Dlr.add
  let postDecrementProperty : ChangeProperty = postChangeProperty Dlr.sub 
    
  //----------------------------------------------------------------------------
  let postIncrementIdentifier ctx name =
    match Identifier.getExprIndexLevelType ctx name with
    | None -> postIncrementProperty ctx ctx.Globals name
    | Some(expr, i, _, tc) -> 
      let var = Expr.unboxIndex expr i tc
      postIncrementVariable ctx var tc