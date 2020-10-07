#time "on"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"
#load "worker.fsx"
open Akka.Actor
open Akka.FSharp
open System
open System.Diagnostics

let system = ActorSystem.Create("Gossip")

let topology = "line"
let protocol = "gossip"

let numNodes = 10
let numResend = 10


type Message = 
    | ReportMsgRecieved of string
    | Result of double * double
    | RecordStartTime of double
    | RecordNumPeople of double
    | Initialize of IActorRef list
    | StartGossip of string
    | StartPushSum of double
    | ComputePushSum of double * double * double




let Listener (mailbox: Actor<_>) = 
    let mutable msgRecieved = 0 |> double
    let mutable startTime = 0 |> double
    let mutable numPeople = 0 |> double
    let rec loop () = actor {
        let! message = mailbox.Receive ()
        match message with
            | ReportMsgRecieved(str)->
                   // let endTime = system.currenttime
                msgRecieved <- msgRecieved + 1.0
                if(msgRecieved = numPeople) then
                    printfn "Time of convergence endtime - startime"

            |  Result(sum,weight) -> 
                 // let endTime = system.currenttime
                printfn "sum %f weight %f average %f" sum weight (sum/weight)
                printfn "Time of convergence endtime - startime"
                Environment.Exit 0

            | RecordStartTime(strTime) ->
                startTime <- strTime

            | RecordNumPeople(noPeople) ->
                numPeople <- noPeople
                
            | _ -> 
                printfn "Invalid Message"
                Environment.Exit 1
           



        return! loop ()
    }
    loop ()


let Node (mailbox: Actor<_>) = 
    let mutable neighbours: IActorRef list = []
    let mutable numMsgHeard = 0
    let mutable sum = nodeNum |> double
    let mutable weight = 1.0
    let mutable termRound = 1
    let rec loop (listener : IActorRef, numResend: int, nodeNum: int) = actor {
        let! message = mailbox.Receive ()
        match message with
            | Initialize(actorRef)->
                neighbours <- actorRef
            |  StartGossip(msg) -> 
                numMsgHeard<- numMsgHeard + 1
                if(numMsgHeard = 1) then
                    listener <! ReportMsgRecieved(msg)

                if (numMsgHeard < 100) then
                    let rand = Random(1234)
                    let selectedNeiIdx = ((rand.Next())|>int) % (neighbours.Length-1) // check
                    neighbours.[selectedNeiIdx] <! StartGossip(msg)

            | StartPushSum(delta) ->
                let rand = Random(1234)
                let selectedNeiIdx = ((rand.Next())|>int) % (neighbours.Length-1)
                sum <- sum /2.0 
                weight <- weight / 2.0
                neighbours.[selectedNeiIdx] <! ComputePushSum(sum, weight, delta)


            | ComputePushSum(s, w, delta) ->
                let mutable newSum = sum + s
                let mutable newWeight = weight + w
                let temp = ((sum/weight) - (newSum/newWeight))
                let finalSum = Math.Abs(temp)
                if(finalSum > delta) then 
                    termRound <- 0
                    sum <- sum + s
                    weight <- weight + w

                    sum <- sum /2.0
                    weight<- weight / 2.0

                    let rand = Random(1234)
                    let selectedNeiIdx = ((rand.Next())|>int) % (neighbours.Length-1)
                    neighbours.[selectedNeiIdx] <! ComputePushSum(sum, weight, delta)

                elif (termRound >=3) then
                    listener <! Result(sum,weight)

                else 
                    let rand = Random(1234)
                    let selectedNeiIdx = ((rand.Next())|>int) % (neighbours.Length-1)
                    sum <- sum /2.0
                    weight<- weight / 2.0
                    neighbours.[selectedNeiIdx] <! ComputePushSum(sum, weight, delta)
                    termRound <- termRound + 1

                    
            | _ -> 
                printfn "Invalid Message"
                Environment.Exit 1

        return! loop ()
    }
    loop ()

let finalArr = Array.zeroCreate(numNodes + 1)
let listener = spawn system "listener" Listener

// let actorRef = spawn system "node" (actorOf2 Node(listener, numResend, i+1))
for i in [0..numNodes-1] do
    finalArr.[i] <- spawn system "node" (actorOf2 (Node(listener, numResend, i+1)))


// for i in [0..numNodes-1] do
//     finalArr.[i] <- spawn system "node1" Node <! listener numNodes i+1
// let listener = system.ActorOf(Props(typedefof<Listener>), name = "listener")
// let workerRef =
//     [ 1 .. numNodes ]
//             |> List.map (fun id -> spawn system (id.ToString()) Node)


// for i in [0..numNodes-1] do
//     let properties =[| listener(IActorRef) :> obj;startNum + workRange - bigint(1) :> obj ;k :> obj;mailbox.Context.Self :> obj |]
//     finalArr.[i] <- system.ActorOf(Props(typedefof<Node>, neighbours))





