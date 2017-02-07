# Neo4jTypeProvider

This is a fork of the original Neo4j Type Provider by Pete Bayne on BitBucket.

[Original Neo4j Type Provider blog post](https://medium.com/@haumohio/the-trips-and-traps-of-creating-a-generative-type-provider-in-f-75162d99622c#.1qsii2nwn)

I forked it and made some changes to make it work with v. 3 of Neo4j.  See my blog post:

[Neo4j Type Provider blog post](https://marnee.silvrback.com/neo4j-type-provider)

Example Usage

First you need to instantiate the type like this. 

```fsharp
    [<Literal>]
    let ConnectionString = @"http://localhost:7474/db/data" //the db should be populated
    type IVRSchema = Haumohio.Neo4j.Schema< ConnectionString >

```

You can get at your graphs labels like this ([see my previous post on the IVR graph](http://marnee.silvrback.com/an-ivr-with-neo4j-and-f-part-1)).

```fsharp
let startLabel = IVRSchema.Labels.START
``` 

You can get at a label's property fields like this.

```fsharp
let endIdField = IVRSchema.Props.END.id
```

You can get at a relationship like this.

```fsharp
let successRelationship = IVRSchema.Rels.SUCCESS
```

You can get a node as an object like this.

```fsharp
let entryNode = IVRSchema.Proxies.ENTRY
```

Putting it all together you can do something like this to get a list of related nodes.  In this case the path is from an entry node to the next entry node through the success relationship.  Here I am using Neo4jClient.

```fsharp
type IVRSchema = Haumohio.Neo4j.Schema< ConnectionString >
let db = new Neo4jClient.GraphClient(Uri(ConnectionString))
db.Connect()

let data = 
    db.Cypher
        .Match(sprintf "(s:%s)-[r:%s]->(e:%s)" IVRSchema.Labels.START IVRSchema.Rels.GOTO IVRSchema.Labels.ENTRY)
        .Return(fun (s:ICypherResultItem) (e:ICypherResultItem) -> (s.As<IVRSchema.Proxies.START>(), e.As<IVRSchema.Proxies.ENTRY>()))
        .Results

let sNode, eNode = data |> Seq.head

```

###Thoughts###

This is a nice way to work with your database's schema and it will naturally update as the source database changes.  

```fsharp
I |> heart TypeProviders
```
