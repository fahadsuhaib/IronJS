﻿namespace IronJS.Native

open System
open IronJS
open IronJS.Runtime
open IronJS.Support.CustomOperators

module Boolean =
  
  ///
  let private constructor' (ctor:FO) (this:CO) (value:BV) =
    let value = TypeConverter.ToBoolean value
    match this with
    | null -> ctor.Env.NewBoolean(value) |> BV.Box
    | _ -> value |> BV.Box
    

  ///
  let setup (env:Env) =
    let ctor = new Func<FO, CO, BV, BV>(constructor')
    let ctor = ctor $ Utils.createConstructor env (Some 1)

    ctor.MetaData.Name <- "Boolean"
    ctor.Put("prototype", env.Prototypes.Boolean, DescriptorAttrs.Immutable)

    env.Globals.Put("Boolean", ctor, DescriptorAttrs.DontEnum)
    env.Constructors.Boolean <- ctor

  ///
  module Prototype =

    ///
    let private valueOf (valueOf:FO) (this:CO) =
      this.CheckType<BO>()
      this |> ValueObject.GetValue

    ///
    let private toString (toString:FO) (this:CO) =
      this.CheckType<BO>()
      this |> ValueObject.GetValue |> TypeConverter.ToString

    ///
    let create (env:Env) objPrototype =
      let prototype = env.NewBoolean()
      prototype.Prototype <- objPrototype
      prototype

    ///
    let setup (env:Env) =
      let proto = env.Prototypes.Boolean;

      proto.Put("constructor", env.Constructors.Boolean, DescriptorAttrs.DontEnum)    
    
      let valueOf = Function(valueOf) $ Utils.createFunction env (Some 0)
      proto.Put("valueOf", valueOf, DescriptorAttrs.DontEnum)

      let toString = FunctionReturn<string>(toString) $ Utils.createFunction env (Some 0)
      proto.Put("toString", toString, DescriptorAttrs.DontEnum)