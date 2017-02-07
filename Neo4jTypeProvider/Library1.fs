namespace Haumohio.Neo4j

open System
open System.IO
open System.Reflection
open Microsoft.FSharp.Core.CompilerServices
open ProviderImplementation.ProvidedTypes
open Haumohio.Neo4j.types
open Haumohio.Neo4j.graph

(*   for interactive: (remember to reset interactive session before building)

#I @"D:\projects\experiments\Neo4jTypeProvider\Neo4jTypeProvider\bin\Debug";;
#r "Neo4jTypeProvider";;
type neo = Haumohio.Neo4j.Schema<"http://localhost:7474/db/data">;;

//simple check using sample data usually packaged with Neo4j

let m = neo.Proxies.Movie();;
m.released <- "2000";;
let rr =  m.released;;

let p = neo.Proxies.Person();;
p.born <- "2001";;
let bb = p.born;;

*)

[<TypeProvider>]
type Neo4jTypeProvider(config: TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces()
    let connectionStringParameter = ProvidedStaticParameter("connectionString", typeof<string>)
    let asm = (Assembly.LoadFrom(config.RuntimeAssembly))
    let schema = makeType asm "Schema"
    
    let schemaCreation = 
        fun (typeName:string) (parameterValues: obj[]) ->
            match parameterValues with 
            | [| :? string as connectionString|] ->
                let connection = graph.Connect(connectionString)
                typeName
                    |> makeType asm 
                    |> addMember (makeIncludedType "Labels" |> addMembers ( connection.labelList |> toStaticProps ) )
                    |> addMember (makeIncludedType "Rels" |> addMembers ( connection.relList |> toStaticProps ))
                    |> addMember (makeIncludedType "Props" |> addMembers (graphTypes connection))
                    |> addMember (makeIncludedType "Proxies" |> addMembers (graphProxies connection))
                    |> addIncludedType

            | _ -> failwith "unexpected parameter values"

    
    do
       this.AddNamespace(ns, [addIncludedType schema])
       

    do schema.DefineStaticParameters( [connectionStringParameter], schemaCreation )

[<assembly:TypeProviderAssembly>]
do ()