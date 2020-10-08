#time "on"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"

open Akka.Actor
open Akka.FSharp
open System.Diagnostics
open System




let system = ActorSystem.Create("Gossip")



let topology = "line"
let protocol = "gossip"

let mutable numNodes = 1000
let numResend = 10

let a = System.Random(1)
type Message = 
    | ReportMsgRecieved of string
    | Result of double * double
    | RecordStartTime of int
    | RecordNumPeople of int
    | Initialize of IActorRef []
    | StartGossip of string
    | StartPushSum of double
    | ComputePushSum of double * double * double

let Listener (mailbox:Actor<_>) = 
    let mutable msgRecieved = 0 
    let mutable startTime = 0 
    let mutable numPeople = 0 
    let rec loop() = actor {
            let! message = mailbox.Receive()
            match message with
                | ReportMsgRecieved(str)->
                    let endTime = System.DateTime.Now.TimeOfDay.Milliseconds
                    msgRecieved <- msgRecieved + 1
                    
                    if(msgRecieved = numPeople) then
                        printfn "Time of convergence is %d ms" (endTime - startTime)
                        Environment.Exit 0

                |  Result(sum,weight) -> 
                    let endTime = System.DateTime.Now.TimeOfDay.Milliseconds
                    printfn "sum %f weight %f average %f" sum weight (sum/weight)
                    printfn "Time of convergence is %i ms" (endTime - startTime)
                    Environment.Exit 0
                   

                | RecordStartTime(strTime) ->
                    startTime <- strTime

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
                    
                    // neighbours|> Seq.iter (fun x -> printf "Neighbours is %O \n " x)
                    
                |  StartGossip(msg) -> 
                    numMsgHeard<- numMsgHeard + 1
                    if(numMsgHeard = 10) then
                        listener <! ReportMsgRecieved(msg)
                    // printfn "numMsgHeard is %i" numMsgHeard
                    if (numMsgHeard < 100) then
                       //neighbours|> Seq.iter (fun x -> printf "Neighbours is %O \n" x)
                       let a = System.Random()
                       let leader = a.Next(0,neighbours.Length) // check
                       neighbours.[leader] <! StartGossip(msg)

                | StartPushSum(delta) ->
                    let leader = a.Next(0,neighbours.Length)
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

                        let a = System.Random(1)
                        let leader = a.Next(0,neighbours.Length)
                        neighbours.[leader] <! ComputePushSum(sum, weight, delta)

                    elif (termRound >=3) then
                        listener <! Result(sum,weight)

                    else 
                        sum <- sum /2.0
                        weight<- weight / 2.0
                        termRound <- termRound + 1
                        let a = System.Random(1)
                        let leader = a.Next(0, neighbours.Length)
                        neighbours.[leader] <! ComputePushSum(sum, weight, delta)
                        
                    
                | _-> ()
            return! loop()

        }
        loop()



let listener = Listener |> spawn system "listener"
match topology  with 
    | "full"->
        let finalArr = Array.zeroCreate(numNodes)
        for i in [0..numNodes-1] do
            finalArr.[i] <- Node listener 10 (i+1) |> spawn system ("Node" + string(i))
        for i in [0..numNodes-1] do 
            finalArr.[i] <! Initialize(finalArr)
        let leader = a.Next(0,numNodes)
        if(protocol = "gossip") then
            listener <! RecordNumPeople(numNodes)
            // let time = DateTimeOffset.Now.ToUnixTimeMilliseconds() |> int
            listener <! RecordStartTime(System.DateTime.Now.TimeOfDay.Milliseconds)
            printfn "Starting protocol Gossip!"
            finalArr.[leader]<! StartGossip("Hello")
        elif(protocol = "pushsum") then
            listener <! RecordStartTime(System.DateTime.Now.TimeOfDay.Milliseconds)
            printfn "Starting protocol Gossip!"
            finalArr.[leader]<! StartPushSum(10.0 ** -10.0)


    | "line" ->
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
        let leader = a.Next(0,numNodes)
        if(protocol ="gossip")then
            listener<!RecordNumPeople(numNodes)
            listener<!RecordStartTime(System.DateTime.Now.TimeOfDay.Milliseconds)
            printfn "Starting Protocol Gossip"
            finalArr.[leader]<!StartGossip("This is Line topology")

        elif(protocol="pushsum")then
            listener <! RecordStartTime(System.DateTime.Now.TimeOfDay.Milliseconds)
            printfn "Starting PushSum for line"
            finalArr.[leader]<!StartPushSum(10.0 ** -10.0)        
    |_-> ()


System.Console.ReadLine() |> ignore            
            
