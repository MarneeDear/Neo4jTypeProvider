module Haumohio.Neo4j.types

open System
open System.IO
open System.Collections.Generic
open Microsoft.FSharp.Core.CompilerServices
open System.Reflection
open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Quotations
open Haumohio.Neo4j.graph

let internal ns = "Haumohio.Neo4j"
let provAsm = ProvidedAssembly(Path.ChangeExtension(Path.GetTempFileName(), ".dll"))



///////////// a few quick helpers to aid functional style

let internal addMember (mi:#MemberInfo) (ty:ProvidedTypeDefinition) = 
    ty.AddMember mi
    ty

let internal addMembers (mi:#MemberInfo list) (ty:ProvidedTypeDefinition) = 
    ty.AddMembersDelayed (fun() -> mi)
    ty

///////////// creators

let internal createStaticProp name =
    ProvidedProperty(name, typeof<string>, IsStatic = true, GetterCode = (fun args -> <@@ name @@>))

let inline makeProvidedPrivateReadonlyField<'T> fieldName =
  let field = ProvidedField(fieldName, typeof<'T>)
  field |> (fun f -> f.SetFieldAttributes(FieldAttributes.Private ))
  field

let mutable private propsByType : (string * ProvidedField) list = []

let myPropsField nameOfEnclosingType  =
    let dic = dict propsByType
    if not( dic.ContainsKey nameOfEnclosingType ) then
        let newdic = (ProvidedField("_myProps", typeof<Dictionary<string,string>>))
        let added = propsByType @ [ (nameOfEnclosingType, newdic)]
        propsByType <- added
    let dic2 = dict propsByType
    dic2.[nameOfEnclosingType]

let internal createInstanceProp name nameOfEnclosingType =
    let field = (myPropsField nameOfEnclosingType)
    ProvidedProperty(
        name, 
        typeof<string>, 
        IsStatic = false,  
        GetterCode = fun args ->
            let fieldGet = Expr.FieldGet(args.[0], field)
            <@@ 
                let dic = (%%fieldGet:Dictionary<string,string>)
                if dic = null then
                    "dictionary not created yet" 
                else
                    let ok = dic.ContainsKey(name)
                    if ok then
                        dic.[name] 
                    else
                        ""
            @@>
        ,SetterCode = fun args ->
            let fieldGet = Expr.FieldGet(args.[0], field)
            <@@ 
                let dic = (%%fieldGet:Dictionary<string,string>)
                if dic <> null then
                    dic.[name] <- (%%args.[1]) 
            @@>
        )

let printDic (dic:Dictionary<string,string>) = 
    if dic = null then
        "dictionary not created yet" 
    else
        let vals =
            dic.Keys
            |> Seq.toArray
            |> Array.map (fun k -> k + ":" + dic.[k])
        "{" + String.Join(", ", vals) + "}"

let internal createSimpleToString nameOfEnclosingType =
    let field = (myPropsField nameOfEnclosingType)
    ProvidedMethod(
        methodName ="ToString", 
        parameters = [],  
        returnType = typeof<string>, 
        InvokeCode = fun args ->
                        let fieldGet = Expr.FieldGet(args.[0], field)
                        <@@ 
                            let dic = (%%fieldGet:Dictionary<string,string>)
                            printDic dic
                        @@>
        )

let internal createCtor nameOfEnclosingType =
    let ctor = ProvidedConstructor(
                parameters = [], 
                InvokeCode = fun args -> 
                    let result = 
                        <@@ 
                            "" :> obj 
                        @@>
                    Expr.FieldSet(args.[0], (myPropsField nameOfEnclosingType), <@@ Dictionary<string,string>() @@> )
                )
    ctor.AddXmlDocDelayed( fun ()-> "Initializes an instance" )
    ctor

let internal makeType asm typeName = 
    ProvidedTypeDefinition( asm, ns, typeName, Some typeof<obj>, IsErased = false)
    

let internal makeIncludedType typename =
    ProvidedTypeDefinition(typename, Some typeof<obj>, IsErased=false)
    
let internal addIncludedType (ty:ProvidedTypeDefinition) =
    provAsm.AddTypes([ty]) 
    ty

///////////// graph to type conversion

let internal toStaticProps (vals:string list) = 
    vals
    |> List.map (fun a -> createStaticProp a )

let internal graphTypes connection =
    (graph.findNodes connection 
    |> List.map 
        (fun name -> 
            makeIncludedType name
                |> addMembers (connection.propList name |> toStaticProps)
        )
    )

let internal graphProxies connection =
    (graph.findNodes connection 
    |> List.map 
        (fun name -> 
            makeIncludedType name
                |> addMember (myPropsField name)
                |> addMembers (graph.propNames name connection |> List.map ( fun nm -> createInstanceProp nm name ))
                |> addMember (createCtor name)
                |> addMember (createSimpleToString name)
        )
    )

