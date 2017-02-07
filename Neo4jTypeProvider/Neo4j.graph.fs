module internal Haumohio.Neo4j.graph

open System
open Neo4jClient
open Newtonsoft.Json

let internal Connect connectionstring = 
    let a = new Neo4jClient.GraphClient(Uri(connectionstring))  //"http://localhost:7474/db/data"
    a.Connect()
    a

let internal propNames (nodeName:string) (neo:GraphClient) = 
    let data = 
        neo.Cypher
            .Match("(n:"+ nodeName + ")")
            .Return<Neo4jClient.Node<string>>("n")
            .Limit(System.Nullable<int>(1))
            .Results
            |> List.ofSeq
            |> List.head
    data.Data
        .Replace(" ", "")
        .Replace(":0","0")
        .Split([|':';',';'{';'}';'\r';'\n';'"'|], System.StringSplitOptions.RemoveEmptyEntries)
        |> Seq.mapi (fun i x -> if i%2 = 0 then Some x else None)
        |> Seq.choose id
        |> List.ofSeq


let internal findNodes (neo:GraphClient): string list=
    neo.Cypher
        .Match("(n)")
        .With("DISTINCT labels(n) as labels")
        .Unwind("labels", "label")
        .ReturnDistinct<string>("label")
        .OrderBy("label")
        .Results
        |> List.ofSeq

let internal findRels (neo:GraphClient): string list=
    neo.Cypher
        .Match("(a)-[r]-(b)")
        .With("DISTINCT type(r) as label")
        .ReturnDistinct<string>("label")
        .OrderBy("label")
        .Results
        |> List.ofSeq

// some extension methods to facilitate flow
type internal Neo4jClient.GraphClient with
    member this.labelList = findNodes this
    member this.relList = findRels this
    member this.propList nodeName = propNames nodeName this