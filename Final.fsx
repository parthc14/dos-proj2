
#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"

open Akka.Actor
open Akka.FSharp
open System

let system = ActorSystem.Create("Gossip")
let random = System.Random(1)

let mutable numNodes =  int(string (fsi.CommandLineArgs.GetValue 1))
let topology = string (fsi.CommandLineArgs.GetValue 2)
let protocol =  string (fsi.CommandLineArgs.GetValue 3)

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
                    
                    msgRecieved <- msgRecieved + 1
                    
                     
                    if (msgRecieved = numPeople) then
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
                    if (numMsgHeard < 1000) then
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
    let nodeArray= Array.zeroCreate (numNodes+1)
    for i in [0..numNodes-1] do
       nodeArray.[i]<-Node listener 10 (i+1) |> spawn system ("Node" + string(i))
    for i in [0..numNodes-1] do
        let neighbourArray=[|nodeArray.[((i-1+numNodes)%(numNodes+1))];nodeArray.[((i+1+numNodes)%(numNodes+1))]|]
        nodeArray.[i]<!Initialize(neighbourArray)
    let leader = random.Next(0,numNodes)
    if protocol="gossip" then
        listener<!RecordNumPeople(numNodes)
        listener<!RecordStartTime(System.Diagnostics.Stopwatch.StartNew())
        printfn "Starting Protocol Gossip"
        nodeArray.[leader]<!StartGossip("This is Line Topology")
    else if protocol="pushsum" then
        listener<!RecordStartTime(System.Diagnostics.Stopwatch.StartNew())
        printfn "Starting Push Sum Protocol for Line"
        nodeArray.[leader]<!StartPushSum(10.0 ** -10.0)


if(topology = "2D") then
    let kLength = int(ceil(sqrt(float(numNodes))))
    let newMatrix = pown kLength 2
    let nodes = Array.zeroCreate(newMatrix)
    let newKLength = newMatrix - 1
    
    for i in [0..newKLength] do
        nodes.[i]<-Node listener 10 (i+1) |> spawn system ("Node" + string(i))

    for i in [0..kLength-1] do
        for j in [0..kLength-1] do
            let mutable neigh:IActorRef[]=[||]
            if j+1<kLength then
                neigh<-(Array.append neigh [|nodes.[i*kLength+j+1]|])
            if j-1>=0 then
                neigh<-Array.append neigh [|nodes.[i*kLength+j-1]|]
            if i-1>=0 then
                neigh <-Array.append neigh [|nodes.[(i-1)*kLength+j]|]
            if i+1<kLength then
                neigh <-(Array.append neigh [|nodes.[(i+1)*kLength+j]|])
            nodes.[i*kLength+j]<!Initialize(neigh)

    let leader = System.Random().Next(0,newMatrix - 1)
    
    if protocol="gossip" then
       listener<!RecordNumPeople(newMatrix - 1)
       listener<!RecordStartTime(System.Diagnostics.Stopwatch.StartNew())
       printfn "Starting Gossip Protocol"
       nodes.[leader]<!StartGossip("Hello World")
    else if protocol="pushsum" then
       listener<!RecordStartTime(System.Diagnostics.Stopwatch.StartNew())
       printfn "Starting Push Sum Protocol"
       nodes.[leader]<!StartPushSum(10.0 ** -10.0) 


if(topology = "imp2D") then
    let kLength = int(ceil(sqrt(float(numNodes))))
    let newMatrix = pown kLength 2
   
    let nodes = Array.zeroCreate(newMatrix)
    let newKLength = newMatrix - 1
    for i in [0..newKLength] do
       nodes.[i]<-Node listener 10 (i+1) |> spawn system ("Node" + string(i))
   
    for m in [0..kLength-1] do
        for n in [0..kLength-1] do
            let mutable neigh:IActorRef[]=[||]
            
            if n+1<kLength then
                neigh<-(Array.append neigh [|nodes.[m*kLength+n+1]|])
            if n-1 >= 0 then
                neigh<-Array.append neigh [|nodes.[m*kLength+n-1]|]
            if m-1 >= 0 then
                neigh <-Array.append neigh [|nodes.[(m-1)*kLength+n]|]
            if m+1 < kLength then
                neigh <- (Array.append neigh [|nodes.[(m+1)*kLength+n]|])
           
            let randNeigh = System.Random().Next(0, newMatrix - 1) 
            neigh <- (Array.append neigh [|nodes.[randNeigh]|])
            
            nodes.[m*kLength+n]<!Initialize(neigh)

    let leader = System.Random().Next(0,newMatrix - 1) 
    if protocol="gossip" then
        listener<!RecordNumPeople(newMatrix - 1)
        listener<!RecordStartTime(System.Diagnostics.Stopwatch.StartNew())
        printfn "Starting Gossip Protocol"
        nodes.[leader]<!StartGossip("Hello World")
    else if protocol="pushsum" then
        listener<!RecordStartTime(System.Diagnostics.Stopwatch.StartNew())
        printfn "Starting Push Sum Protocol"
        nodes.[leader]<!StartPushSum(10.0 ** -10.0)



System.Console.ReadLine() |> ignore