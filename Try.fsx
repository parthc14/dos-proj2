#time "on"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"

open Akka.Actor
open Akka.FSharp
open System

let system = ActorSystem.Create("Gossip")
let random = System.Random(1)

let mutable numNodes = int(string (fsi.CommandLineArgs.GetValue 1))
let topology = string (fsi.CommandLineArgs.GetValue 2)
let protocol = string (fsi.CommandLineArgs.GetValue 3)

type Message = 
    | ReportMsgRecieved of string
    | Result of double * double
    | RecordStartTime of Diagnostics.Stopwatch
    | RecordNumPeople of int
    | Initialize of IActorRef []
    | StartGossip of string
    | StartPushSum of double
    | ComputePushSum of double * double * double

let Listener (mailbox:Actor<_>) = 
    let mutable msgRecieved = 0 
    let mutable startTime = System.Diagnostics.Stopwatch.StartNew() 
    let mutable numPeople = 0 
    let rec loop() = actor {
            let! message = mailbox.Receive()
            match message with
                | ReportMsgRecieved(str)->
                    let endTime = startTime.Stop()
                    msgRecieved <- msgRecieved + 1
                    if(msgRecieved = numPeople) then
                        printfn "Time of convergence is %f ms" (startTime.Elapsed.TotalMilliseconds)
                        Environment.Exit 0

                |  Result(sum,weight) -> 
                    let endTime = startTime.Stop()
                    printfn "Sum %f \nWeight %f \nAverage %f \n" sum weight (sum/weight)
                    printfn "Time of convergence is %f ms" (startTime.Elapsed.TotalMilliseconds)
                    Environment.Exit 0
                  
                | RecordStartTime(strTime) ->
                    startTime <- System.Diagnostics.Stopwatch.StartNew()

                | RecordNumPeople(noPeople) ->
                    numPeople <- noPeople

                | _-> ()

            return! loop()
        }
    loop()



let Node (listener : ICanTell) (numResend: int) (nodeNum: int)(mailBox:Actor<_>) =
   
        let mutable  neighbours:IActorRef[]=[||]
        let mutable numMsgHeard = 0
        let mutable sum = nodeNum |> double
        let mutable weight = 1.0
        let mutable termRound = 1

        let rec loop() = actor {
            let! message = mailBox.Receive()
            match message with
                | Initialize(actorRef)->
                    neighbours <- actorRef
                    
                |  StartGossip(msg) -> 
                    numMsgHeard<- numMsgHeard + 1
                    if(numMsgHeard = 10) then
                        listener <! ReportMsgRecieved(msg)
                    if (numMsgHeard < 100) then
                       let leader = random.Next(0,neighbours.Length)
                       neighbours.[leader] <! StartGossip(msg)

                | StartPushSum(delta) ->
                    let leader = random.Next(0,neighbours.Length)
                    sum <- sum /2.0 
                    weight <- weight / 2.0
                    neighbours.[leader] <! ComputePushSum(sum, weight, delta)

                | ComputePushSum(s:double, w, delta) ->
                    let newSum = sum + s
                    let newWeight = weight + w
                    let finalSum = ((sum/weight) - (newSum/newWeight)) |> abs
                    if(finalSum > delta) then 
                        termRound <- 0
                        sum <- sum + s
                        weight <- weight + w

                        sum <- sum /2.0
                        weight<- weight / 2.0

                        let leader = random.Next(0,neighbours.Length)
                        neighbours.[leader] <! ComputePushSum(sum, weight, delta)
                    elif (termRound >=3) then
                        listener <! Result(sum,weight)
                    else
                        sum <- sum /2.0
                        weight<- weight / 2.0
                        termRound <- termRound + 1

                        let leader = random.Next(0, neighbours.Length)
                        neighbours.[leader] <! ComputePushSum(sum, weight, delta)
                       
                | _-> ()
            return! loop()

        }
        loop()


let listener = Listener |> spawn system "listener"

// FULL TOPOLOGY
if(topology = "full") then
    let finalArr = Array.zeroCreate(numNodes)
    for i in [0..numNodes-1] do
        finalArr.[i] <- Node listener 10 (i+1) |> spawn system ("Node" + string(i))
    for i in [0..numNodes-1] do 
        finalArr.[i] <! Initialize(finalArr)
    let leader = random.Next(0,numNodes)
    if(protocol = "gossip") then
        listener <! RecordNumPeople(numNodes)
        listener <! RecordStartTime(System.Diagnostics.Stopwatch.StartNew())
        printfn "Starting Gossip protocol"
        finalArr.[leader]<! StartGossip("Hello")
    elif(protocol = "pushsum") then
        listener <! RecordStartTime(System.Diagnostics.Stopwatch.StartNew())
        printfn "Starting Pushsum protocol"
        finalArr.[leader]<! StartPushSum(10.0 ** -10.0)
    else 
        printfn "Invalid Protocol"


// LINE TOPOLOGY
if(topology = "line") then
    let finalArr = Array.zeroCreate(numNodes)
    for i in [0..numNodes-1] do
        finalArr.[i] <- Node listener 10 (i+1) |> spawn system ("Node" + string(i))

    let finalList = finalArr |> Array.toList
    let mutable  neighbours:IActorRef[]=[||]
    let mutable nei: IActorRef list = []

    [ 0 .. numNodes-1]   
        |> List.iter (fun x ->
            if x = 0 then
                nei <- [ finalList.[1] ] |> List.append nei
            elif x = numNodes-1 then 
                nei <- [ finalList.[numNodes-2] ] |> List.append nei
            else 
                nei <- [ finalList.[x-1]; finalList.[x+1] ] |> List.append nei
    )
    for i in [0..numNodes-1] do    
        neighbours<-nei |> List.toArray
        finalArr.[i]<!Initialize(neighbours)             
    let leader = random.Next(0,numNodes)
    if(protocol = "gossip")then
        listener<!RecordNumPeople(numNodes)
        listener<!RecordStartTime(System.Diagnostics.Stopwatch.StartNew())
        printfn "Starting Protocol Gossip"
        finalArr.[leader]<!StartGossip("This is Line topology")
    elif(protocol="pushsum")then
        listener <! RecordStartTime(System.Diagnostics.Stopwatch.StartNew())
        printfn "Starting PushSum for line"
        finalArr.[leader]<!StartPushSum(10.0 ** -10.0)        
    else
        printfn "Invalid Protocol"



System.Console.ReadLine() |> ignore