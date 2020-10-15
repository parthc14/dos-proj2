#time "on"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"

open Akka.Actor
open Akka.FSharp
open System



let random = System.Random(1)
let system = ActorSystem.Create("Fsharp")


let algorithm = "gossip"
let topology = "2D"
let mutable numNodes = 10


type Message = 
    | Converged
    | CalcValue of double * double
    | Rumour
    | WakeUp
    | HeardRumour
    | Dead of IActorRef
    | Number of int
    | Neigh of IActorRef list


let Timer (mailbox:Actor<_>) = 
    let mutable counter = 0
    let mutable numNodes = 0
    let mutable startTime = System.Diagnostics.Stopwatch.StartNew() 
    let rec loop() = actor {
        let! message = mailbox.Receive()
        match message with
            | HeardRumour ->
                    counter <- counter + 1
                    if(counter = numNodes) then
                        startTime.Stop()
                        printfn "Time of convergence is %f ms" (startTime.Elapsed.TotalMilliseconds)
                        Environment.Exit 0

            |  Converged -> 
                    counter <- counter + 1
                    if(counter = numNodes) then
                        startTime.Stop()
                        printfn "Time of convergence is %f ms" (startTime.Elapsed.TotalMilliseconds)
                        Environment.Exit 0
                  
            | Number(n) ->
                    numNodes <- n
                    startTime <- System.Diagnostics.Stopwatch.StartNew() 
                    printfn "Num nodes are %i" numNodes
                    
            | _-> ()
        return! loop()
    }
    loop()



let GossipNode (timer : IActorRef) (mailBox:Actor<_>) =
    let mutable id = -1
    let mutable counter = 0
    let mutable neighbours: IActorRef list = []
    let mutable len = 0

    let rec loop() = actor {
        let! message = mailBox.Receive()
        match message with
            | Neigh(llist) ->
                neighbours <- llist
                len <- neighbours.Length
               
            | Number(i) ->
                id <- i

            | Rumour ->
                
                counter <- counter + 1
                
                if counter = 1 then
                    timer <! HeardRumour
                    neighbours.[random.Next(0,neighbours.Length)] <! Rumour
                    mailBox.Self <! WakeUp
                else        
                    neighbours.[random.Next(0,neighbours.Length)] <! Rumour


            | WakeUp -> 
                if counter<10 then         
                    neighbours.[random.Next(0,neighbours.Length)] <! Rumour
            
            | Dead(a)->         
                let mutable list1: IActorRef list = [a]
                neighbours <- neighbours |> List.except list1
                len <- neighbours.Length
        
            | _-> ()
        return! loop()

    }
    loop()



let timer = Timer |> spawn system "timer"
let mutable actorList: IActorRef list = []


if algorithm ="gossip" then
    for i in [0 .. numNodes-1] do
        let mutable actor = GossipNode timer |> spawn system ("GossipNode" + string(i))
        actor <! Number(i)
        printfn "Sending messages"
        actorList <- actorList @ [actor]

    

if topology = "2D" then
    let numOfNodes = 10 |> float
    let k = sqrt numOfNodes |> int
    let noToSkip = k*k
    let limit = k*k-1



    actorList.[0] <! [actorList.[1] ; actorList.[k]]
    actorList.[k-1] <! [actorList.[k-2] ; actorList.[2*k-1]]
    actorList.[limit] <! [actorList.[limit-k] ; actorList.[limit-1]]
    actorList.[limit-k+1] <! [actorList.[limit-k+2]; actorList.[limit+1-2*k]]


    for i in [1 .. limit] do
        if i<>0 && i<>k-1 && i<>limit && i<>limit-k+1 then
            if i<k then
                actorList.[i] <! [actorList.[i-1]; actorList.[i+1]; actorList.[i+k]]
            elif i > limit-k then
                actorList.[i] <! [actorList.[i-1]; actorList.[i+1]; actorList.[i-k]]
             elif (i%k = 0) then
                actorList.[i] <! [actorList.[i+1]; actorList.[i+k]; actorList.[i-k]]
            elif i%k = k-1 then
                actorList.[i] <! [actorList.[i-1]; actorList.[i+k]; actorList.[i-k]]
            else
                actorList.[i] <! [actorList.[i-1]; actorList.[i+1]; actorList.[i-k]; actorList.[i+k]] 





if algorithm = "gossip" then
    actorList.[0] <! Rumour
elif algorithm = "push-sum" then
    actorList.[0] <! CalcValue(0.0,0.0)
else 
    printfn "Invalid algorithm"



if topology = "2D" || topology = "imp2D" then
    let numOfNodes = numNodes |> float
    let a = sqrt numOfNodes |> int
    let b = pown a 2 
    timer <! b