﻿namespace IronJS.Api

open System
open IronJS
open IronJS.Aliases

//------------------------------------------------------------------------------
// Static class containing all type conversions
//------------------------------------------------------------------------------
type TypeConverter =

  //----------------------------------------------------------------------------
  static member toBox(b:Box byref) = b
  static member toBox(d:double) = Utils.boxDouble d
  static member toBox(b:bool) = Utils.boxBool b
  static member toBox(s:string) = Utils.boxString s
  static member toBox(o:IjsObj) = Utils.boxObject o
  static member toBox(f:IjsFunc) = Utils.boxFunction f
  static member toBox(c:HostObject) = Utils.boxClr c
  static member toBox(expr:Dlr.Expr) = 
    Dlr.callStaticT<TypeConverter> "toBox" [expr]

  //----------------------------------------------------------------------------
  static member toString (b:bool) = if b then "true" else "false"
  static member toString (s:string) = s
  static member toString (u:Undefined) = "undefined"
  static member toString (b:Box) =
    match b.Type with
    | TypeCodes.Empty -> "undefined"
    | TypeCodes.String -> b.String
    | TypeCodes.Bool -> TypeConverter.toString b.Bool
    | TypeCodes.Number -> TypeConverter.toString b.Double
    | TypeCodes.Clr -> TypeConverter.toString b.Clr
    | TypeCodes.Undefined -> TypeConverter.toString b.Undefined
    | TypeCodes.Object -> TypeConverter.toString b.Object
    | TypeCodes.Function -> TypeConverter.toString (b.Func :> IjsObj)
    | _ -> Errors.Generic.invalidTypeCode b.Type

  static member toString (o:IjsObj) = 
    TypeConverter.toString(Object.defaultValue(o, DefaultValue.String))

  static member toString (expr:Dlr.Expr) =
    Dlr.callStaticT<TypeConverter> "toString" [expr]

  static member toString (d:double) = 
    if System.Double.IsInfinity d then "Infinity" else d.ToString()

  static member toString (c:HostObject) = 
    if c = null then "null" else c.ToString()
      
  //----------------------------------------------------------------------------
  static member toPrimitive (b:bool, _:byte) = Utils.boxBool b
  static member toPrimitive (d:double, _:byte) = Utils.boxDouble d
  static member toPrimitive (s:string, _:byte) = Utils.boxString s
  static member toPrimitive (u:Undefined, _:byte) = Utils.boxUndefined u
  static member toPrimitive (o:IjsObj, h:byte) = Object.defaultValue(o, h)
  static member toPrimitive (b:Box, h:byte) =
    match b.Type with
    | TypeCodes.Bool
    | TypeCodes.Number
    | TypeCodes.String
    | TypeCodes.Empty
    | TypeCodes.Undefined -> b
    | TypeCodes.Clr -> TypeConverter.toPrimitive(b.Clr, h)
    | TypeCodes.Object
    | TypeCodes.Function -> Object.defaultValue(b.Object, h)
    | _ -> Errors.Generic.invalidTypeCode b.Type
  
  static member toPrimitive (c:HostObject, _:byte) = 
    Utils.boxClr (if c = null then null else c.ToString())

  static member toPrimitive (expr:Dlr.Expr) =
    Dlr.callStaticT<TypeConverter> "toPrimitive" [expr]
      
  //----------------------------------------------------------------------------
  static member toBoolean (b:bool) = b
  static member toBoolean (d:double) = d > 0.0 || d < 0.0
  static member toBoolean (c:HostObject) = if c = null then false else true
  static member toBoolean (s:string) = s.Length > 0
  static member toBoolean (u:Undefined) = false
  static member toBoolean (o:IjsObj) = true
  static member toBoolean (b:Box) =
    match b.Type with 
    | TypeCodes.Bool -> b.Bool
    | TypeCodes.Number -> TypeConverter.toBoolean b.Double
    | TypeCodes.String -> b.String.Length > 0
    | TypeCodes.Empty
    | TypeCodes.Undefined -> false
    | TypeCodes.Clr -> TypeConverter.toBoolean b.Clr
    | TypeCodes.Object 
    | TypeCodes.Function -> true
    | _ -> Errors.Generic.invalidTypeCode b.Type
    
  static member toBoolean (expr:Dlr.Expr) =
    Dlr.callStaticT<TypeConverter> "toBoolean" [expr]

  //----------------------------------------------------------------------------
  static member toNumber (b:bool) : double = if b then 1.0 else 0.0
  static member toNumber (d:double) = d
  static member toNumber (c:HostObject) = if c = null then 0.0 else 1.0
  static member toNumber (u:Undefined) = Number.NaN
  static member toNumber (o:IjsObj) : Number = Errors.Generic.notImplemented()
  static member toNumber (b:Box byref) =
    match b.Type with
    | TypeCodes.Number -> b.Double
    | TypeCodes.Bool -> if b.Bool then 1.0 else 0.0
    | TypeCodes.String -> TypeConverter.toNumber(b.String)
    | TypeCodes.Empty
    | TypeCodes.Undefined -> System.Double.NaN
    | TypeCodes.Clr -> TypeConverter.toNumber b.Clr
    | TypeCodes.Object 
    | TypeCodes.Function -> TypeConverter.toNumber(b.Object)
    | _ -> Errors.Generic.invalidTypeCode b.Type

  static member toNumber (expr:Dlr.Expr) = 
    Dlr.callStaticT<TypeConverter> "toNumber" [expr]

  static member toNumber (s:string) = 
    let mutable d = 0.0
    if Double.TryParse(s, anyNumber, invariantCulture, &d) 
      then d
      else NaN
        
  //----------------------------------------------------------------------------
  static member toObject (o:IjsObj) = o
  static member toObject (b:Box byref) =
    match b.Type with
    | TypeCodes.Function
    | TypeCodes.Object -> b.Object
    | _ -> Errors.Generic.invalidTypeCode b.Type

  static member toObject (expr:Dlr.Expr) =
    Dlr.callStaticT<TypeConverter> "toObject" [expr]
      
  //----------------------------------------------------------------------------
  static member toInt32 (d:double) = int d
  static member toUInt32 (d:double) = uint32 d
  static member toUInt16 (d:double) = uint16 d
  static member toInteger (d:double) : double = 
    if d = NaN
      then 0.0
      elif d = 0.0 || d = NegInf || d = PosInf
        then d
        else double (Math.Sign(d)) * Math.Floor(Math.Abs(d))
                
  //-------------------------------------------------------------------------
  static member convertTo (expr:Dlr.Expr) (t:System.Type) =
    if Object.ReferenceEquals(expr.Type, t) then expr
    elif t.IsAssignableFrom(expr.Type) then Dlr.cast t expr
    else 
      if   t = typeof<IjsNum> then TypeConverter.toNumber expr
      elif t = typeof<IjsStr> then TypeConverter.toString expr
      elif t = typeof<IjsBool> then TypeConverter.toBoolean expr
      elif t = typeof<IjsBox> then TypeConverter.toBox expr
      elif t = typeof<IjsObj> then TypeConverter.toObject expr
      else Errors.Generic.noConversion expr.Type t

  static member convertToT<'a> expr = 
    TypeConverter.convertTo expr typeof<'a>

and Operators =

  static member add_String_Number (s:string, n:Number) =
    s + TypeConverter.toString(n)

  static member add_Box_Number (b:Box byref, n:Number) =
    let mutable r = Box()
    match b.Type with
    | TypeCodes.String -> 
      r.Type <- TypeCodes.String
      r.String <- Operators.add_String_Number(b.String, n)

    | _ ->
      let b = TypeConverter.toNumber(&b)
      r.Type <- TypeCodes.Number
      r.Double <- b + n
    r

  static member typeOf (b:Box) = TypeCodes.Names.[b.Type]
  static member typeOf expr = Dlr.callStaticT<Operators> "typeOf" [expr]

  static member lt (b:Box byref, n:Number) = TypeConverter.toNumber(&b) < n
  static member lt (n:Number, b:Box byref) = n < TypeConverter.toNumber(&b)
  static member lt (l, r) = Dlr.callStaticT<Operators> "lt" [l; r]

  static member ltEq (b:Box byref, n:Number) = TypeConverter.toNumber(&b) <= n
  static member ltEq (n:Number, b:Box byref) = n <= TypeConverter.toNumber(&b)
  static member ltEq (l, r) = Dlr.callStaticT<Operators> "ltEq" [l; r]

  static member gt (b:Box byref, n:Number) = TypeConverter.toNumber(&b) > n
  static member gt (n:Number, b:Box byref) = n > TypeConverter.toNumber(&b)
  static member gt (l, r) = Dlr.callStaticT<Operators> "gt" [l; r]

  static member gtEq (b:Box byref, n:Number) = TypeConverter.toNumber(&b) >= n
  static member gtEq (n:Number, b:Box byref) = n >= TypeConverter.toNumber(&b)
  static member gtEq (l, r) = Dlr.callStaticT<Operators> "gtEq" [l; r]

  static member eq (b:Box byref, n:Number) = TypeConverter.toNumber(&b) = n
  static member eq (n:Number, b:Box byref) = n = TypeConverter.toNumber(&b)
  static member eq (l, r) = Dlr.callStaticT<Operators> "eq" [l; r]

  static member notEq (b:Box byref, n:Number) = TypeConverter.toNumber(&b) <> n
  static member notEq (n:Number, b:Box byref) = n <> TypeConverter.toNumber(&b)
  static member notEq (l, r) = Dlr.callStaticT<Operators> "notEq" [l; r]


//-------------------------------------------------------------------------
// PropertyClass API
//-------------------------------------------------------------------------
and PropertyClass =
        
  //-----------------------------------------------------------------------
  static member subClass (x:IronJS.PropertyClass, name) = 
    if x.Id < 0L then failwith "Can't subclass dynamic PropertyClasses"
    let mutable subClass = null
      
    if not(x.SubClasses.TryGetValue(name, &subClass)) then
      let newMap = new MutableDict<string, int>(x.PropertyMap)
      newMap.Add(name, newMap.Count)
      subClass <- IronJS.PropertyClass(x.Env, newMap)
      x.SubClasses.Add(name, subClass)

    subClass

  //-----------------------------------------------------------------------
  static member subClass (x:IronJS.PropertyClass, names:string seq) =
    Seq.fold (fun c (n:string) -> PropertyClass.subClass(c, n)) x names
        
  //-----------------------------------------------------------------------
  static member makeDynamic (x:IronJS.PropertyClass) =
    if x.Id < 0L then failwith "PropertyClass is already dynamic"
    let pc = new IronJS.PropertyClass(null)
    pc.Id <- -1L
    pc.NextIndex <- x.NextIndex
    pc.FreeIndexes <- new MutableStack<int>()
    pc.PropertyMap <- new MutableDict<string, int>(x.PropertyMap)
    pc
        
  //-----------------------------------------------------------------------
  static member addDynamic (x:IronJS.PropertyClass, name) =
    if x.Id >= 0L then 
      failwith "Can't add dynamic name to a non-dynamic PropertyClass"

    let index = 
      if x.FreeIndexes.Count > 0 then x.FreeIndexes.Pop()
      else x.NextIndex <- x.NextIndex + 1; x.NextIndex - 1

    x.PropertyMap.Add(name, index)
    index
        
  //-----------------------------------------------------------------------
  static member delete (x:IronJS.PropertyClass, name) =
    let pc = if x.Id >= 0L then PropertyClass.makeDynamic x else x
    let index = pc.PropertyMap.[name]

    if pc.PropertyMap.Remove name then 
      pc.FreeIndexes.Push index

    pc
      
  //-----------------------------------------------------------------------
  static member getIndex (x:IronJS.PropertyClass, name) =
    x.PropertyMap.[name]
    
//-------------------------------------------------------------------------
// Environment API
//-------------------------------------------------------------------------
and Environment =
  static member addCompiler (x:IjsEnv, funId, compiler) =
    if not (x.Compilers.ContainsKey funId) then
      x.Compilers.Add(funId, CachedCompiler compiler)
  
  static member hasCompiler (x:IjsEnv, funcId) =
    x.Compilers.ContainsKey funcId
    
//-------------------------------------------------------------------------
// Function API
//-------------------------------------------------------------------------
and Function =

  static member call (f:IjsFunc, t) =
    let c = f.Compiler.compileAs<Func<IjsFunc,IjsObj,IjsBox>>(f)
    c.Invoke(f, t)

  static member call (f:IjsFunc, t, a0:'a) =
    let c = f.Compiler.compileAs<Func<IjsFunc,IjsObj,'a,IjsBox>>(f)
    c.Invoke(f, t, a0)

  static member call (f:IjsFunc, t, a0:'a, a1:'b) =
    let c = f.Compiler.compileAs<Func<IjsFunc,IjsObj,'a,'b,IjsBox>>(f)
    c.Invoke(f, t, a0, a1)

  static member call (f:IjsFunc, t, a0:'a, a1:'b, a2:'c) =
    let c = f.Compiler
    let c = c.compileAs<Func<IjsFunc,IjsObj,'a,'b,'c,IjsBox>>(f)
    c.Invoke(f, t, a0, a1, a2)

  static member call (f:IjsFunc, t, a0:'a, a1:'b, a2:'c, a3:'d) =
    let c = f.Compiler
    let c = c.compileAs<Func<IjsFunc,IjsObj,'a,'b,'c,'d,IjsBox>>(f)
    c.Invoke(f, t, a0, a1, a2, a3)

  static member call (f:IjsFunc, t, a0:'a, a1:'b, a2:'c, a3:'d, a4:'e) =
    let c = f.Compiler
    let c = c.compileAs<Func<IjsFunc,IjsObj,'a,'b,'c,'d,'e,IjsBox>>(f)
    c.Invoke(f, t, a0, a1, a2, a3, a4)

  static member call (f:IjsFunc, t, a0:'a, a1:'b, a2:'c, a3:'d, a4:'e, a5:'f) =
    let c = f.Compiler
    let c = c.compileAs<Func<IjsFunc,IjsObj,'a,'b,'c,'d,'e,'f,IjsBox>>(f)
    c.Invoke(f, t, a0, a1, a2, a3, a4, a5)
    
//-------------------------------------------------------------------------
// Delegate Function API
//-------------------------------------------------------------------------
and DelegateFunction<'a when 'a :> Delegate> =
  
  //-----------------------------------------------------------------------
  static member compile (x:IjsFunc) (type':System.Type) =
    let f = x :?> IronJS.DelegateFunction<'a>
    let argTypes = Reflection.getDelegateArgTypes type'
    let args = 
      argTypes |> Array.mapi (fun i x -> Dlr.param (sprintf "~parm%i" i) x) 

    let realArgs =
      if f.ArgTypes.Length >= 2 
        && f.ArgTypes.[0] = TypeObjects.Function
        && f.ArgTypes.[1] = TypeObjects.Object then
        args |> Array.ofSeq

      elif f.ArgTypes.Length >= 1
        && f.ArgTypes.[0] = TypeObjects.Object then
        args |> Seq.skip 1 |> Array.ofSeq

      else
        args |> Seq.skip 2 |> Array.ofSeq

    let convertedArgs = 
      f.ArgTypes |> Seq.mapi (fun i t -> TypeConverter.convertTo realArgs.[i] t)
        
    let casted = Dlr.castT<IronJS.DelegateFunction<'a>> args.[0]
    let invoke = Dlr.invoke (Dlr.field casted "Delegate") convertedArgs

    let body = 
      if Utils.isBox f.ReturnType then invoke
      elif Utils.isVoid f.ReturnType then Expr.voidAsUndefined invoke
      else
        Dlr.blockTmpT<Box> (fun tmp ->
          [
            (Expr.setBoxTypeOf tmp invoke)
            (Expr.setBoxValue tmp invoke)
            (tmp :> Dlr.Expr)
          ] |> Seq.ofList
        )
            
    let lambda = Dlr.lambda type' args body
    #if INTERACTIVE
    Dlr.Utils.printDebugView lambda
    #else
    #if DEBUG
    Dlr.Utils.printDebugView lambda
    #endif
    #endif
    lambda.Compile()
      
  //-----------------------------------------------------------------------
  static member create (env:IronJS.Environment, delegate':'a) =
    let x = IronJS.DelegateFunction<'a>(env, delegate')

    Environment.addCompiler(
      env, (x :> IjsFunc).FunctionId, (DelegateFunction<'a>.compile)
    )

    (x :> IjsFunc).Compiler <- env.Compilers.[(x :> IjsFunc).FunctionId]
    x


        
//------------------------------------------------------------------------------
// GetPropertyCache API
//------------------------------------------------------------------------------
and GetPropertyCache =
  static member update (x:IronJS.GetPropertyCache, o:IjsObj) =
    match o.PropertyClassId with
    | -1L -> Object.getProperty(o, x.PropertyName)
    | _   -> 
      let mutable h = null
      let mutable i = -1

      if Object.hasProperty(o, x.PropertyName, &h, &i) then
        if Object.ReferenceEquals(o, h) then
          x.PropertyIndex   <- i
          x.PropertyClassId <- h.PropertyClassId
        else
          () //Build prototype crawler

        h.PropertyValues.[i]

      else
        Utils.boxedUndefined

        
//------------------------------------------------------------------------------
// InvokeCache API
//------------------------------------------------------------------------------
and InvokeCache =
  static member update (x:IronJS.InvokeCache<'a>, f:IjsFunc) =
    x.FunctionId <- f.FunctionId
    x.Cached <- f.Compiler.compileAs<'a> f
    x.Cached
      
  static member create (t:HostType) =
    typedefof<InvokeCache<_>>
      .MakeGenericType([|t|])
      .GetConstructor([||])
      .Invoke([||])


            
//------------------------------------------------------------------------------
// PutPropertyCache API
//------------------------------------------------------------------------------
and PutPropertyCache =
  static member update (x:IronJS.PutPropertyCache, o:IjsObj, value:Box) =
    match o.PropertyClassId with
    | PropertyClassTypes.Dynamic -> 
      Object.putProperty(o, x.PropertyName, value)
    | _   -> 
      let mutable i = -1
      if Object.getOwnPropertyIndex(o, x.PropertyName, &i) then
        x.PropertyIndex   <- i
        x.PropertyClassId <- o.PropertyClassId
      Object.putProperty(o, x.PropertyName, value)

  static member update (x:IronJS.PutPropertyCache, o:IjsObj, value:IjsNum) =
    match o.PropertyClassId with
    | PropertyClassTypes.Dynamic -> 
      Object.putProperty(o, x.PropertyName, value)
    | _   -> 
      let mutable i = -1
      if Object.getOwnPropertyIndex(o, x.PropertyName, &i) then
        x.PropertyIndex   <- i
        x.PropertyClassId <- o.PropertyClassId
      Object.putProperty(o, x.PropertyName, value)

  static member update (x:IronJS.PutPropertyCache, o:IjsObj, value:IjsStr) =
    match o.PropertyClassId with
    | PropertyClassTypes.Dynamic -> 
      Object.putProperty(o, x.PropertyName, value)
    | _   -> 
      let mutable i = -1
      if Object.getOwnPropertyIndex(o, x.PropertyName, &i) then
        x.PropertyIndex   <- i
        x.PropertyClassId <- o.PropertyClassId
      Object.putProperty(o, x.PropertyName, value)

  static member update (x:IronJS.PutPropertyCache, o:IjsObj, value:IjsObj) =
    match o.PropertyClassId with
    | PropertyClassTypes.Dynamic -> 
      Object.putProperty(o, x.PropertyName, value)

    | _   -> 
      let mutable i = -1
      if Object.getOwnPropertyIndex(o, x.PropertyName, &i) then
        x.PropertyIndex   <- i
        x.PropertyClassId <- o.PropertyClassId
      Object.putProperty(o, x.PropertyName, value)

  static member update (x:IronJS.PutPropertyCache, o:IjsObj, value:IjsFunc) =
    match o.PropertyClassId with
    | PropertyClassTypes.Dynamic -> 
      Object.putProperty(o, x.PropertyName, value)

    | _   -> 
      let mutable i = -1
      if Object.getOwnPropertyIndex(o, x.PropertyName, &i) then
        x.PropertyIndex   <- i
        x.PropertyClassId <- o.PropertyClassId
      Object.putProperty(o, x.PropertyName, value)
        

      
//------------------------------------------------------------------------------
// Object API
//------------------------------------------------------------------------------
and Object() =

  //----------------------------------------------------------------------------
  // 8.6.2.6 - [[DefaultValue]]
  static member defaultValue (x:IjsObj) : Box =
    match x.Class with
    | Classes.Date -> Object.defaultValue(x, DefaultValue.String)
    | _ -> Object.defaultValue(x, DefaultValue.Number)
    
  //----------------------------------------------------------------------------
  // 8.6.2.6 - [[DefaultValue]]
  static member defaultValue (x:IjsObj, hint:byte) : Box =
    let valueOf = Object.getProperty(x, "valueOf")
    let toString = Object.getProperty(x, "toString")

    match hint with
    | DefaultValue.Number ->
      match valueOf.Type with
      | TypeCodes.Function -> 
        let mutable v = Function.call(valueOf.Func, x)
        if Utils.isPrimitive &v then v
        else
          match toString.Type with
          | TypeCodes.Function ->
            let mutable v = Function.call(toString.Func, x)
            if Utils.isPrimitive &v then v else Errors.runtime "[[TypeError]]"
          | _ -> Errors.runtime "[[TypeError]]"
      | _ -> Errors.runtime "[[TypeError]]"

    | DefaultValue.String ->
      match toString.Type with
      | TypeCodes.Function ->
        let mutable v = Function.call(toString.Func, x)
        if Utils.isPrimitive &v then v
        else 
          match toString.Type with
          | TypeCodes.Function ->
            let mutable v = Function.call(valueOf.Func, x)
            if Utils.isPrimitive &v then v else Errors.runtime "[[TypeError]]"
          | _ -> Errors.runtime "[[TypeError]]"
      | _ -> Errors.runtime "[[TypeError]]"

    | _ -> Errors.runtime "Invalid hint"
    
  //----------------------------------------------------------------------------
  // Methods that deal with property access
  //----------------------------------------------------------------------------

  //----------------------------------------------------------------------------
  //Makes the Object and its PropertyClass dynamic
  static member makeDynamic (x:IjsObj) =
    x.PropertyClass <- PropertyClass.makeDynamic x.PropertyClass
    x.PropertyClassId <- x.PropertyClass.Id

  //----------------------------------------------------------------------------
  //Expands PropertyValue array size
  static member expandPropertyStorage (x:IjsObj) =
    let newPropertyValues = 
      Array.zeroCreate (if x.count = 0 then 2 else x.count*2)

    if x.count > 0 then 
      Array.Copy(x.PropertyValues, newPropertyValues, x.PropertyValues.Length)

    x.PropertyValues <- newPropertyValues

  //----------------------------------------------------------------------------
  //Creates a property index for 'name' if one doesn't exist
  static member createPropertyIndex (x:IjsObj, name:string) =
    let mutable i = -1
    if not (Object.getOwnPropertyIndex(x, name, &i)) then
      i <- ( 
        if x.PropertyClassId >= 0L then
          x.PropertyClass <- PropertyClass.subClass(x.PropertyClass, name)
          x.PropertyClassId <- x.PropertyClass.Id
          x.PropertyClass.PropertyMap.[name]

        //Dynamic
        else
          PropertyClass.addDynamic(x.PropertyClass, name)
      )

      if x.isFull then Object.expandPropertyStorage x
    i
      
  //----------------------------------------------------------------------------
  //Checks for a property, including Prototype chain
  static member hasProperty (x, name, obj:IjsObj byref, index:int byref) =
    obj <- x
    let mutable continue' = true

    while continue' && not (Object.ReferenceEquals(obj, null)) do
      if obj.PropertyClass.PropertyMap.TryGetValue(name, &index)
        then continue'  <- false  //Found
        else obj        <- obj.Prototype //Try next in chain

    not continue'
      
  //----------------------------------------------------------------------------
  //Checks for a property, including Prototype chain
  static member hasProperty (x:IjsObj, name:string) =
    let mutable o = null
    let mutable i = -1
    Object.hasProperty(x, name, &o, &i)
      
  //----------------------------------------------------------------------------
  //Gets a property value, including Prototype chain
  static member getProperty (x:IjsObj, name:string) =
    let mutable h = null
    let mutable i = -1
    if Object.hasProperty(x, name, &h, &i) 
      then h.PropertyValues.[i]
      else Utils.boxedUndefined
      
  //----------------------------------------------------------------------------
  //Gets the index for a property named 'name'
  static member getOwnPropertyIndex (x:IjsObj, name:string, out:int byref) =
    x.PropertyClass.PropertyMap.TryGetValue(name, &out)
      
  //----------------------------------------------------------------------------
  //Gets all property names for the current object
  static member getOwnPropertyNames (x:IjsObj) =
    seq {for x in x.PropertyClass.PropertyMap.Keys -> x}
    
  //----------------------------------------------------------------------------
  static member getOwnPropertyAttributes (x:IjsObj, i:int) =
    if not (Utils.refEquals x.PropertyAttributes null) 
     && i < x.PropertyAttributes.Length then 
      x.PropertyAttributes.[i]
    else
      0s
      
  //----------------------------------------------------------------------------
  //Deletes a property on the object, making it dynamic in the process
  static member deleteOwnProperty (x:IjsObj, name:string) =
    let mutable i = -1
    if Object.getOwnPropertyIndex(x, name, &i) then
      let attrs = Object.getOwnPropertyAttributes(x, i)
      if attrs &&& PropertyAttrs.DontDelete = 0s then
        x.PropertyClass <- PropertyClass.delete(x.PropertyClass, name)
        x.PropertyClassId <- x.PropertyClass.Id

        x.PropertyValues.[i].Clr <- null
        x.PropertyValues.[i].Type <- TypeCodes.Empty
        x.PropertyValues.[i].Double <- 0.0
          
        true //We managed to delete the property

      else false
    else false

  //----------------------------------------------------------------------------
  //Special method for "length"-property due to arrays
  static member putLength (x:IjsObj, value:double) =
    if x.Class = Classes.Array then

      let newLength = TypeConverter.toUInt32 value

      if (double newLength) = value then
        if Utils.isDense x then
          while x.IndexLength > newLength do
            Object.deleteIndex(x, x.IndexLength - 1u) |> ignore
            x.IndexLength <- x.IndexLength - 1u

        else
          for k in List.ofSeq (x.IndexSparse.Keys) do
            if k >= newLength then 
              x.IndexSparse.Remove k |> ignore
          x.IndexLength <- newLength

      else
        failwith "RangeError"

    Object.putProperty(x, "length", value)

  //----------------------------------------------------------------------------
  static member putProperty(x:IjsObj, n:string, value:obj, attrs:int16) =
    let i = Object.createPropertyIndex(x, n)
    let tc = Utils.obj2tc value

    match tc with
    | TypeCodes.Bool -> x.PropertyValues.[i].Bool <- unbox value
    | TypeCodes.Number -> x.PropertyValues.[i].Double <- unbox value
    | _ -> x.PropertyValues.[i].Clr <- value

    x.PropertyValues.[i].Type <- tc

  //----------------------------------------------------------------------------
  //Puts a property value
  static member putProperty (x:IjsObj, name:string, value:Box) =
    let i = Object.createPropertyIndex(x, name)
    x.PropertyValues.[i] <- value
    value //Return
      
  static member putProperty (x:IjsObj, name:string, value:bool) =
    let index = Object.createPropertyIndex(x, name)
    Utils.setBoolInArray x.PropertyValues index value
    value //Return

  static member putProperty (x:IjsObj, name:string, value:double) =
    let index = Object.createPropertyIndex(x, name)
    Utils.setNumberInArray (x.PropertyValues) index value
    value //Return

  static member putProperty (x:IjsObj, name:string, value:HostObject) =
    let index = Object.createPropertyIndex(x, name)
    Utils.setClrInArray x.PropertyValues index value
    value //Return

  static member putProperty (x:IjsObj, name:string, value:string) =
    let index = Object.createPropertyIndex(x, name)
    Utils.setStringInArray x.PropertyValues index value
    value //Return

  static member putProperty (x:IjsObj, name:string, value:Undefined) =
    let index = Object.createPropertyIndex(x, name)
    Utils.setUndefinedInArray x.PropertyValues index value
    value //Return

  static member putProperty (x:IjsObj, name:string, value:IjsObj) =
    let index = Object.createPropertyIndex(x, name)
    Utils.setObjectInArray x.PropertyValues index value
    value //Return

  static member putProperty (x:IjsObj, name:string, value:IjsFunc) =
    let index = Object.createPropertyIndex(x, name)
    Utils.setFunctionInArray x.PropertyValues index value
    value //Return
        
  //-------------------------------------------------------------------------
  //
  // Methods that deal with the indexing operators: foo[0], foo["bar"], etc.
  //
  //-------------------------------------------------------------------------
      
  //-------------------------------------------------------------------------
  //Expands IndexValues array size
  static member expandIndexStorage (x:IjsObj, index) =
    if x.IndexValues = null || x.IndexValues.Length <= index then
      let size = if index >= 1073741823 then 2147483647 else ((index+1) * 2)
      let newIndexValues = Array.zeroCreate size
      
      if x.IndexValues <> null && x.IndexValues.Length > 0 then
        Array.Copy(x.IndexValues, newIndexValues, x.IndexValues.Length)

      x.IndexValues <- newIndexValues
        
  //-------------------------------------------------------------------------
  //Changes the index storage to be more efficient for sparse indexes
  static member initSparse (x:IjsObj) =
    if Utils.isDense x then
      x.IndexSparse <- new MutableSorted<uint32, Box>()

      for i = 0 to (int (x.IndexLength-1u)) do
        if x.IndexValues.[i].Type <> TypeCodes.Empty then
          x.IndexSparse.Add(uint32 i, x.IndexValues.[i])

      x.IndexValues <- null

  //-------------------------------------------------------------------------
  //Box Indexers
  static member putIndex (x:IjsObj, index:Box byref, value:Box) = 
    match index.Type with
    | TypeCodes.Number -> Object.putIndex(x, index.Double, value)
    | TypeCodes.String -> Object.putIndex(x, index.String, value)
    | _ -> failwith "Que?"

  static member putIndex (x:IjsObj, index:Box byref, value:bool) = 
    match index.Type with
    | TypeCodes.Number -> Object.putIndex(x, index.Double, value)
    | TypeCodes.String -> Object.putIndex(x, index.String, value)
    | _ -> failwith "Que?"

  static member getIndex (x:IjsObj, index:Box byref) =
    match index.Type with
    | TypeCodes.Number -> Object.getIndex(x, index.Double)
    | TypeCodes.String -> Object.getIndex(x, index.String)
    | _ -> failwith "Que?"

  static member hasIndex (x:IjsObj, index:Box byref) =
    match index.Type with
    | TypeCodes.Number -> Object.hasIndex(x, index.Double)
    | TypeCodes.String -> Object.hasIndex(x, index.String)
    | _ -> failwith "Que?"
      
  //-------------------------------------------------------------------------
  //String Indexers

  static member putIndex (x:IjsObj, index:IjsStr, value:Box) = 
    let mutable i = Index.Min
    if Utils.isStringIndex(index, &i) 
      then Object.putIndex(x, i, value)
      else 
        if x.Class=Classes.Array && index="length" 
          then Object.putLength(x, TypeConverter.toNumber value); value
          else Object.putProperty(x, index, value)

  static member putIndex (x:IjsObj, index:IjsStr, value:bool) = 
    let mutable i = Index.Min
    if Utils.isStringIndex(index, &i) 
      then Object.putIndex(x, i, value)
      else 
        if x.Class=Classes.Array && index="length" 
          then Object.putLength(x, TypeConverter.toNumber value); value
          else Object.putProperty(x, index, value)

  static member putIndex (x:IjsObj, index:IjsStr, value:IjsNum) = 
    let mutable i = Index.Min
    if Utils.isStringIndex(index, &i) 
      then Object.putIndex(x, i, value)
      else 
        if x.Class=Classes.Array && index="length" 
          then Object.putLength(x, TypeConverter.toNumber value)
          else Object.putProperty(x, index, value)

  static member putIndex (x:IjsObj, index:IjsStr, value:HostObject) = 
    let mutable i = Index.Min
    if Utils.isStringIndex(index, &i) 
      then Object.putIndex(x, i, value)
      else 
        if x.Class=Classes.Array && index="length" 
          then Object.putLength(x, TypeConverter.toNumber value); value
          else Object.putProperty(x, index, value)

  static member putIndex (x:IjsObj, index:IjsStr, value:IjsStr) = 
    let mutable i = Index.Min
    if Utils.isStringIndex(index, &i) 
      then Object.putIndex(x, i, value)
      else 
        if x.Class=Classes.Array && index="length" 
          then Object.putLength(x, TypeConverter.toNumber value); value
          else Object.putProperty(x, index, value)

  static member putIndex (x:IjsObj, index:IjsStr, value:Undefined) = 
    let mutable i = Index.Min
    if Utils.isStringIndex(index, &i) 
      then Object.putIndex(x, i, value)
      else 
        if x.Class=Classes.Array && index="length" 
          then Object.putLength(x, TypeConverter.toNumber value); value
          else Object.putProperty(x, index, value)

  static member putIndex (x:IjsObj, index:IjsStr, value:IjsObj) = 
    let mutable i = Index.Min
    if Utils.isStringIndex (index, &i) 
      then Object.putIndex(x, i, value)
      else 
        if x.Class=Classes.Array && index="length" 
          then Object.putLength(x, TypeConverter.toNumber value); value
          else Object.putProperty(x, index, value)

  static member putIndex (x:IjsObj, index:IjsStr, value:IjsFunc) = 
    let mutable i = Index.Min
    if Utils.isStringIndex(index, &i) 
      then Object.putIndex(x, i, value)
      else 
        if x.Class=Classes.Array && index="length" 
          then Object.putLength(x, TypeConverter.toNumber value); value
          else Object.putProperty(x, index, value)

  static member getIndex (x:IjsObj, index:IjsStr) = 
    let mutable i = Index.Min
    if Utils.isStringIndex(index, &i) 
      then Object.getIndex(x, i)
      else Object.getProperty(x, index)

  static member hasIndex (x:IjsObj, index:IjsStr) = 
    let mutable i = Index.Min
    if Utils.isStringIndex(index, &i) 
      then Object.hasIndex(x, i)
      else Object.hasProperty(x, index)

  static member deleteIndex (x:IjsObj, index:IjsStr) =
    let mutable i = Index.Min
    if Utils.isStringIndex(index, &i)
      then Object.deleteIndex(x, i)
      else Object.deleteOwnProperty(x, index)
        
  //----------------------------------------------------------------------------
  // IjsNum indexers
  //----------------------------------------------------------------------------
  static member putIndex (x:IjsObj, index:IjsNum, value:Box) = 
    let i = uint32 index
    if double i = index
      then Object.putIndex(x, i, value)
      else Object.putProperty(x, TypeConverter.toString index, value)

  static member putIndex (x:IjsObj, index:IjsNum, value:IjsBool) = 
    let i = uint32 index
    if double i = index
      then Object.putIndex(x, i, value)
      else Object.putProperty(x, TypeConverter.toString index, value)

  static member putIndex (x:IjsObj, index:IjsNum, value:IjsNum) = 
    let i = uint32 index
    if double i = index
      then Object.putIndex(x, i, value)
      else Object.putProperty(x, TypeConverter.toString index, value)

  static member putIndex (x:IjsObj, index:IjsNum, value:HostObject) = 
    let i = uint32 index
    if double i = index
      then Object.putIndex(x, i, value)
      else Object.putProperty(x, string index, value)

  static member putIndex (x:IjsObj, index:IjsNum, value:IjsStr) = 
    let i = uint32 index
    if double i = index
      then Object.putIndex(x, i, value)
      else Object.putProperty(x, TypeConverter.toString index, value)

  static member putIndex (x:IjsObj, index:IjsNum, value:Undefined) = 
    let i = uint32 index
    if double i = index
      then Object.putIndex(x, i, value)
      else Object.putProperty(x, TypeConverter.toString index, value)

  static member putIndex (x:IjsObj, index:IjsNum, value:IjsObj) = 
    let i = uint32 index
    if double i = index
      then Object.putIndex(x, i, value)
      else Object.putProperty(x, TypeConverter.toString index, value)

  static member putIndex (x:IjsObj, index:IjsNum, value:IjsFunc) = 
    let i = uint32 index
    if double i = index
      then Object.putIndex(x, i, value)
      else Object.putProperty(x, TypeConverter.toString index, value)

  static member getIndex (x:IjsObj, index:IjsNum) = 
    let i = uint32 index
    if double i = index
      then Object.getIndex(x, i)
      else Object.getProperty(x, TypeConverter.toString index)

  static member hasIndex (x:IjsObj, index:IjsNum) = 
    let i = uint32 index
    if double i = index
      then Object.hasIndex(x, i)
      else Object.hasProperty(x, TypeConverter.toString index)

  static member deleteIndex (x:IjsObj, index:IjsNum) =
    let i = uint32 index
    if double i = index
      then Object.deleteIndex(x, i)
      else Object.deleteOwnProperty(x, TypeConverter.toString index)

  //-------------------------------------------------------------------------
  //UInt32 Indexers
  static member putIndex (x:IjsObj, ui:uint32, value:Box) = 
    if ui > Index.Max then Object.initSparse x
    if not (Object.ReferenceEquals(x.IndexSparse, null)) then
      x.IndexSparse.[ui] <- value
      if ui >= x.IndexLength then x.IndexLength <- ui + 1u
      value
    else
      if ui >= x.IndexLength then 
        if x.Class = Classes.Array then
          Object.putProperty(x, "length", double x.IndexLength) |> ignore
        if ui >= 255u && ui/2u >= x.IndexLength then
          Object.initSparse x
          Object.putIndex(x, ui, value)
        else
          x.IndexLength <- ui + 1u
          Object.expandIndexStorage(x, int ui)
          x.IndexValues.[int ui] <- value; value
      else x.IndexValues.[int ui] <- value; value

  static member putIndex (x:IjsObj, ui:uint32, value:bool) = 
    if ui > Index.Max then Object.initSparse x
    if not (Object.ReferenceEquals(x.IndexSparse, null)) then
      x.IndexSparse.[ui] <- Utils.boxBool value
      if ui >= x.IndexLength then x.IndexLength <- ui + 1u
      value
    else
      if ui >= x.IndexLength then 
        if x.Class = Classes.Array then
          Object.putProperty(x, "length", double x.IndexLength) |> ignore
        if ui >= 255u && ui/2u >= x.IndexLength then
          Object.initSparse x
          Object.putIndex(x, ui, value)
        else
          x.IndexLength <- ui + 1u
          Object.expandIndexStorage(x, int ui)
          Utils.setBoolInArray x.IndexValues (int ui) value; value
      else Utils.setBoolInArray x.IndexValues (int ui) value; value

  static member putIndex (x:IjsObj, ui:uint32, value:double) = 
    if ui > Index.Max then Object.initSparse x
    if not (Object.ReferenceEquals(x.IndexSparse, null)) then
      x.IndexSparse.[ui] <- Utils.boxDouble value
      if ui >= x.IndexLength then x.IndexLength <- ui + 1u
      value
    else
      if ui >= x.IndexLength then 
        if x.Class = Classes.Array then
          Object.putProperty(x, "length", double x.IndexLength) |> ignore
        if ui >= 255u && ui/2u >= x.IndexLength then
          Object.initSparse x
          Object.putIndex(x, ui, value)
        else
          x.IndexLength <- ui + 1u
          Object.expandIndexStorage(x, int ui)
          Utils.setNumberInArray x.IndexValues (int ui) value; value
      else Utils.setNumberInArray x.IndexValues (int ui) value; value

  static member putIndex (x:IjsObj, ui:uint32, value:HostObject) : HostObject = 
    if ui > Index.Max then Object.initSparse x
    if not (Object.ReferenceEquals(x.IndexSparse, null)) then
      x.IndexSparse.[ui] <- Utils.boxClr value
      if ui >= x.IndexLength then x.IndexLength <- ui + 1u
      value
    else
      if ui >= x.IndexLength then 
        if x.Class = Classes.Array then
          Object.putProperty(x, "length", double x.IndexLength) |> ignore

        if ui >= 255u && ui/2u >= x.IndexLength then
          Object.initSparse x
          Object.putIndex(x, ui, value)
        else
          x.IndexLength <- ui + 1u
          Object.expandIndexStorage(x, int ui)
          Utils.setClrInArray x.IndexValues (int ui) value; value
      else Utils.setClrInArray x.IndexValues (int ui) value; value

  static member putIndex (x:IjsObj, ui:uint32, value:string) = 
    if ui > Index.Max then Object.initSparse x
    if not (Object.ReferenceEquals(x.IndexSparse, null)) then
      x.IndexSparse.[ui] <- Utils.boxString value
      if ui >= x.IndexLength then x.IndexLength <- ui + 1u
      value
    else
      if ui >= x.IndexLength then 
        if x.Class = Classes.Array then
          Object.putProperty(x, "length", double x.IndexLength) |> ignore
        if ui >= 255u && ui/2u >= x.IndexLength then
          Object.initSparse x
          Object.putIndex(x, ui, value)
        else
          x.IndexLength <- ui + 1u
          Object.expandIndexStorage(x, int ui)
          Utils.setStringInArray x.IndexValues (int ui) value; value
      else Utils.setStringInArray x.IndexValues (int ui) value; value

  static member putIndex (x:IjsObj, ui:uint32, value:Undefined) = 
    if ui > Index.Max then Object.initSparse x
    if not (Object.ReferenceEquals(x.IndexSparse, null)) then
      x.IndexSparse.[ui] <- Utils.boxedUndefined
      if ui >= x.IndexLength then x.IndexLength <- ui + 1u
      value
    else
      if ui >= x.IndexLength then 
        if x.Class = Classes.Array then
          Object.putProperty(x, "length", double x.IndexLength) |> ignore
        if ui >= 255u && ui/2u >= x.IndexLength then
          Object.initSparse x
          Object.putIndex(x, ui, value)
        else
          x.IndexLength <- ui + 1u
          Object.expandIndexStorage(x, int ui)
          Utils.setUndefinedInArray x.IndexValues (int ui) value; value
      else Utils.setUndefinedInArray x.IndexValues (int ui) value; value

  static member putIndex (x:IjsObj, ui:uint32, value:IjsObj) = 
    if ui > Index.Max then Object.initSparse x
    if not (Object.ReferenceEquals(x.IndexSparse, null)) then
      x.IndexSparse.[ui] <- Utils.boxObject value
      if ui >= x.IndexLength then x.IndexLength <- ui + 1u
      value
    else
      if ui >= x.IndexLength then 
        if x.Class = Classes.Array then
          Object.putProperty(x, "length", double x.IndexLength) |> ignore
        if ui >= 255u && ui/2u >= x.IndexLength then
          Object.initSparse x
          Object.putIndex(x, ui, value)
        else
          x.IndexLength <- ui + 1u
          Object.expandIndexStorage(x, int ui)
          Utils.setObjectInArray x.IndexValues (int ui) value; value
      else Utils.setObjectInArray x.IndexValues (int ui) value; value

  static member putIndex (x:IjsObj, ui:uint32, value:IjsFunc) = 
    if ui > Index.Max then Object.initSparse x
    if not (Object.ReferenceEquals(x.IndexSparse, null)) then
      x.IndexSparse.[ui] <- Utils.boxFunction value
      if ui >= x.IndexLength then x.IndexLength <- ui + 1u
      value
    else
      if ui >= x.IndexLength then 
        if x.Class = Classes.Array then
          Object.putProperty(x, "length", double x.IndexLength) |> ignore
        if ui >= 255u && ui/2u >= x.IndexLength then
          Object.initSparse x
          Object.putIndex(x, ui, value)
        else
          x.IndexLength <- ui + 1u
          Object.expandIndexStorage(x, int ui)
          Utils.setFunctionInArray x.IndexValues (int ui) value; value
      else Utils.setFunctionInArray x.IndexValues (int ui) value; value

  static member getIndex (x:IjsObj, ui:uint32) = 
    let mutable o = null
    if Object.hasIndex(x, &o, ui) then
      if Object.ReferenceEquals(o.IndexSparse, null)
        then o.IndexValues.[int ui]
        else o.IndexSparse.[ui]
    else
      Utils.boxedUndefined

  static member hasIndex (x:IjsObj, i:uint32) =
    let mutable o = null
    Object.hasIndex(x, &o, i)

  static member hasIndex (x:IjsObj, o:IjsObj byref, ui:uint32) =
    o <- x
      
    let i = int ui
    let mutable continue' = true

    while continue' && not (Object.ReferenceEquals(o, null)) do
      if ui >= o.IndexLength then
        o <- o.Prototype

      elif not (Object.ReferenceEquals(o.IndexValues, null)) 
          && o.IndexValues.[i].Type <> TypeCodes.Empty then

        continue' <- false

      elif not (Object.ReferenceEquals(o.IndexSparse, null)) 
        && o.IndexSparse.ContainsKey ui then

        continue' <- false

      else 
        o <- o.Prototype

    not continue'

  static member deleteIndex (x:IjsObj, ui:uint32) : bool =
    if ui < x.IndexLength then 
      if not(Object.ReferenceEquals(x.IndexSparse, null)) then
        x.IndexSparse.Remove(ui) |> ignore
      else
        let i = int ui
        x.IndexValues.[i].Clr <- null
        x.IndexValues.[i].Type <- TypeCodes.Empty
      true
    else
      false