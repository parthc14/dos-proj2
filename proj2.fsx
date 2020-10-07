#time "on"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"
open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.TestKit
open System.Collections.Generic


let system = ActorSystem.Create("FSharp")

let bigInt (x: int) = bigint (x)

let args : string array = fsi.CommandLineArgs |> Array.tail
let N =  args.[0] |> float
let k = args.[1] |> float |> int |> bigint


let workRange = N / 30.0 |> float |> ceil |> int |> bigInt
let n= N |> int |> bigint
let mutable notEnd = true



type ActorCreator(startNum: bigint, endNum: bigint, k: bigint, caller: IActorRef) =
    inherit Actor()
    do
        let mutable one = 1 |> bigInt
        let mutable start = 1|> bigint
        let mutable answer = 0 |> bigInt 
        for i in [startNum..endNum] do
            let windowSize = (i+k-bigInt(1))
            for j in [i..windowSize] do
                    one <- (j*j)
                    answer <- answer + one
            while(start*start <=(answer)) do
                if(start*start = answer) then
                    printfn "%A\n" (windowSize-k + bigint(1))

                start<- start + bigint(1)
            answer <- bigint(0) 
        caller.Tell("done")
        ()
    override x.OnReceive message = ()



let BossActor =
    spawn system "EchoServer"
    <| fun mailbox ->
      
        let mutable numActors = 0

        let rec spawnChild (startNum: bigint, workRange: bigint, k: bigint, numOfActors: bigint, n: bigint) =
            if (numOfActors = bigint(1) || startNum + workRange - bigint(1) > n) then
                let properties =
                    [| startNum :> obj;n :> obj;k :> obj;mailbox.Context.Self :> obj |]

                let actorRef =
                    system.ActorOf(Props(typedefof<ActorCreator>, properties))

                numActors <- numActors + 1
                ()
            else
                let properties =
                    [| startNum :> obj;startNum + workRange - bigint(1) :> obj ;k :> obj;mailbox.Context.Self :> obj |]

                let actorRef =
                    system.ActorOf(Props(typedefof<ActorCreator>, properties))

                numActors <- numActors + 1
                spawnChild (startNum + workRange, workRange, k, numOfActors - bigint(1), n)

        spawnChild (bigint(1), workRange, k, bigint(30), n)
        ()
        let rec loop () =
            actor {
                let! message = mailbox.Receive()
                match box message with
                | :? string as msg ->
                    numActors <- numActors - 1
                    if (numActors > 0) then return! loop () else 
                    notEnd <- false
                    ()
                | _ -> failwith "unknown message"
            }
        loop ()

while notEnd do
    ignore

system.Terminate()|>ignore