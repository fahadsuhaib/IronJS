﻿namespace IronJS.Compiler

open IronJS
open IronJS.Compiler
open IronJS.Dlr.Operators

///
module Identifier =

  ///
  let getVariableStorage (name:string) (ctx:Ctx)  =

    let rec walkSharedChain n expr =
      if n = 0 then expr
      else
        let expr = Dlr.index0 expr .-> "Scope"
        expr |> walkSharedChain (n-1)

    match ctx.Variables |> Map.tryFind name with
    | Some variable ->
      match variable with
      | Ast.Shared(storageIndex, globalLevel, closureLevel) ->
        let closureDifference = ctx.ClosureLevel - closureLevel

        let expr =
          ctx.Parameters.SharedScope
          |> walkSharedChain closureDifference

        Some(expr, storageIndex, globalLevel)

      | Ast.Private(storageIndex) ->
        let globalLevel = ctx.Scope |> Ast.Utils.globalLevel
        Some(ctx.Parameters.PrivateScope :> Dlr.Expr, storageIndex, globalLevel)

    | None ->
      None

  ///
  let getDynamicArgs (ctx:Ctx) name =
    match ctx |> getVariableStorage name with
    | None -> [Dlr.int_1; ctx.Globals; Dlr.defaultT<Scope>; Dlr.int_1]
    | Some(expr, index, level) ->
      [Dlr.const' level; ctx.Globals; expr; Dlr.const' index]

  ///
  let private getValueDynamic (ctx:Ctx) name =
    let defaultArgs = [Dlr.const' name; ctx.Parameters.DynamicScope :> Dlr.Expr]
    let dynamicArgs = getDynamicArgs ctx name
    let args = defaultArgs @ dynamicArgs
    Dlr.callStaticT<DynamicScopeHelpers> "Get" args

  ///
  let private setValueDynamic (ctx:Ctx) name value =
    let defaultArgs = [Dlr.const' name; Utils.box value; ctx.Parameters.DynamicScope :> Dlr.Expr]
    let dynamicArgs = getDynamicArgs ctx name
    let args = defaultArgs @ dynamicArgs
    Dlr.callStaticT<DynamicScopeHelpers> "Set" args

  ///
  let isGlobal (ctx:Ctx) name =
    ctx.Variables |> Map.containsKey name |> not

  ///
  let getValue (ctx:Ctx) name =
    match ctx.DynamicLookup with
    | true -> getValueDynamic ctx name
    | _ ->
      match ctx |> getVariableStorage name with
      | Some(expr, i, _) -> Dlr.indexInt expr i
      | _ -> Object.getMember ctx ctx.Globals name true

  ///
  let setValue (ctx:Ctx) name value =
    match ctx.DynamicLookup with
    | true -> setValueDynamic ctx name value
    | _ ->
      match ctx |> getVariableStorage name with
      | None ->
        Object.putMember ctx ctx.Globals name value

      | Some(expr, i, _) ->
        let varExpr = (Dlr.indexInt expr i)
        Utils.assign varExpr value
