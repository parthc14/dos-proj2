#if INTERACTIVE
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 
#endif

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open System.Collections.Generic

type Gossip =
    |Initailize of IActorRef[]
    |StartGossip of String
    |ReportMsgRecvd of String
    |StartPushSum of Double
    |ComputePushSum of Double * Double * Double
    |Result of Double * Double
    |RecordStartTime of int
    |RecordNumPeople of int
let rnd  = System.Random(1)
type Listener() =
    inherit Actor()
    let mutable msgRecieved = 0
    let mutable startTime = 0
    let mutable numPeople =0

    override x.OnReceive(rmsg) = 
        match rmsg :?>Gossip with 
        | ReportMsgRecvd message ->
            let endTime = System.DateTime.Now.TimeOfDay.Milliseconds
            msgRecieved <- msgRecieved + 1
            if msgRecieved = numPeople then
                printfn "Hearing Actor %i" msgRecieved
                printfn "Time for convergence: %i ms" (endTime-startTime)
                Environment.Exit(0)  
           

        | Result (sum,weight) ->
            let endTime = System.DateTime.Now.TimeOfDay.Milliseconds
            printfn "Sum = %f Weight= %f Average=%f" sum weight (sum/weight) 
            printfn "Time for convergence: %i ms" (endTime-startTime)
            Environment.Exit(0)
        | RecordStartTime strtTime ->
            startTime <-strtTime

        | RecordNumPeople numpeople ->
            numPeople <-numpeople
        | _->()
 
type Node(listener: IActorRef, numResend: int, nodeNum: int)=
    inherit Actor()
    let mutable numMsgHeard = 0 
    let mutable  neighbours:IActorRef[]=[||]

    //used for push sum
    let mutable sum1= nodeNum |> float
    let mutable weight = 1.0
    let mutable termRound = 1

    
 
    override x.OnReceive(num)=
         // printfn "Avik %A"
         match num :?>Gossip with 
         | Initailize aref->
                neighbours<-aref

         | StartGossip msg ->
                numMsgHeard<- numMsgHeard+1
                // printfn "Hearing Actor %s %i" Actor.Context.Self.Path.Name numMsgHeard
                if(numMsgHeard=10) then 
                      listener <! ReportMsgRecvd(msg)
                
                if(numMsgHeard <=100) then
                        let index= System.Random().Next(0,neighbours.Length)
                        neighbours.[index] <! StartGossip(msg)

         | StartPushSum delta -> 
                        let index= System.Random().Next(0,neighbours.Length)
                        sum1<- sum1/2.0
                        weight <-weight/2.0
                        neighbours.[index] <! ComputePushSum(sum1,weight,delta)

         | ComputePushSum (s:float,w,delta) -> 
                          let  newsum = sum1+s
                          let newweight = weight + w
                          let cal = sum1/weight - newsum/newweight |> abs
                          if(cal >delta) then
                            termRound<- 0
                            sum1 <- sum1+s
                            weight <- weight + w
                            sum1 <- sum1/2.0
                            weight <- weight/2.0
                            let index= System.Random().Next(0,neighbours.Length)
                            neighbours.[index] <! ComputePushSum(sum1,weight,delta)
                           elif (termRound>=3) then
                             listener<! Result(sum1,weight)
                            else
                               sum1<- sum1/2.0
                               weight <- weight/2.0
                               termRound<- termRound+1
                               let index= System.Random().Next(0,neighbours.Length)
                               neighbours.[index] <! ComputePushSum(sum1,weight,delta)


         | _-> ()


let mutable numOfNodes = int(string (fsi.CommandLineArgs.GetValue 1))
let topology = string (fsi.CommandLineArgs.GetValue 2)
let protocol = string (fsi.CommandLineArgs.GetValue 3)

let system = ActorSystem.Create("System")

let mutable actualNumOfNodes=float(numOfNodes)

numOfNodes = if topology="2D" || topology="Imp2D" then 
                 int(floor((actualNumOfNodes ** 0.5) ** 2.0))
             else
                 numOfNodes
          
let listener=system.ActorOf(Props.Create(typeof<Listener>),"listener")

match topology  with 
      | "full"->
          let nodeArray= Array.zeroCreate (numOfNodes+1)
          for i in [0..numOfNodes] do
              nodeArray.[i]<-system.ActorOf(Props.Create(typeof<Node>,listener,10,i+1),"demo"+string(i))
          for i in [0..numOfNodes] do
              nodeArray.[i]<!Initailize(nodeArray)
              
          let leader = System.Random().Next(0,numOfNodes)
          if protocol="gossip" then
            listener<!RecordNumPeople(numOfNodes)
            listener<!RecordStartTime(System.DateTime.Now.TimeOfDay.Milliseconds)
            printfn "Starting Protocol Gossip"
            nodeArray.[leader]<!StartGossip("Hello")
          else if protocol="push-sum" then
            listener<!RecordStartTime(System.DateTime.Now.TimeOfDay.Milliseconds)
            printfn "Starting Push Protocol"
            nodeArray.[leader]<!StartPushSum(10.0 ** -10.0)
      |"line"->
          let nodeArray= Array.zeroCreate (numOfNodes+1)
          for i in [0..numOfNodes] do
              nodeArray.[i]<-system.ActorOf(Props.Create(typeof<Node>,listener,10,i+1),"demo"+string(i))
          for i in [0..numOfNodes] do
              let neighbourArray=[|nodeArray.[((i-1+numOfNodes)%(numOfNodes+1))];nodeArray.[((i+1+numOfNodes)%(numOfNodes+1))]|]
              nodeArray.[i]<!Initailize(neighbourArray)
          let leader = System.Random().Next(0,numOfNodes)
          if protocol="gossip" then
            listener<!RecordNumPeople(numOfNodes)
            listener<!RecordStartTime(System.DateTime.Now.TimeOfDay.Milliseconds)
            printfn "Starting Protocol Gossip"
            nodeArray.[leader]<!StartGossip("This is Line Topology")
          else if protocol="push-sum" then
            listener<!RecordStartTime(System.DateTime.Now.TimeOfDay.Milliseconds)
            printfn "Starting Push Sum Protocol for Line"
            nodeArray.[leader]<!StartPushSum(10.0 ** -10.0)

      |"2D"->
           let gridSize=int(ceil(sqrt actualNumOfNodes))
           let totGrid=gridSize*gridSize
           let nodeArray= Array.zeroCreate (totGrid)
           for i in [0..(gridSize*gridSize-1)] do
              nodeArray.[i]<-system.ActorOf(Props.Create(typeof<Node>,listener,10,i+1),"demo"+string(i))
           
           for i in [0..gridSize-1] do
               for j in [0..gridSize-1] do
                    let mutable neighbours:IActorRef[]=[||]
                    if j+1<gridSize then
                        neighbours<-(Array.append neighbours [|nodeArray.[i*gridSize+j+1]|])
                    if j-1>=0 then
                        neighbours<-Array.append neighbours [|nodeArray.[i*gridSize+j-1]|]
                    if i-1>=0 then
                        neighbours<-Array.append neighbours [|nodeArray.[(i-1)*gridSize+j]|]
                    if i+1<gridSize then
                        neighbours<-(Array.append neighbours [|nodeArray.[(i+1)*gridSize+j]|])
                    nodeArray.[i*gridSize+j]<!Initailize(neighbours)

           let leader = System.Random().Next(0,totGrid-1)  
           if protocol="gossip" then
            listener<!RecordNumPeople(totGrid-1)
            listener<!RecordStartTime(System.DateTime.Now.TimeOfDay.Milliseconds)
            printfn "Starting Protocol Gossip"
            nodeArray.[leader]<!StartGossip("This is 2D Topology")
           else if protocol="push-sum" then
            listener<!RecordStartTime(System.DateTime.Now.TimeOfDay.Milliseconds)
            printfn "Starting Push Sum Protocol for Line"
            nodeArray.[leader]<!StartPushSum(10.0 ** -10.0)

      |"Imp2D"->
           let kLength = int(ceil(sqrt(float(numOfNodes))))
           let newMatrix = pown kLength 2
           
           let nodes = Array.zeroCreate(newMatrix)
           
           let newKLength = newMatrix - 1
           for i in [0..newKLength] do
              nodes.[i]<-system.ActorOf(Props.Create(typeof<Node>,listener,i+1),"demo"+string(i))
           
           for m in [0..kLength-1] do
               for n in [0..kLength-1] do
                    let mutable neigh:IActorRef[]=[||]
                    
                    if n + 1 < kLength then
                        neigh<- Array.append neigh [|nodes.[m*kLength+n+1]|]
                    if n-1 >= 0 then
                        neigh <- Array.append neigh [|nodes.[m*kLength+n-1]|]
                    if m-1 >= 0 then
                        neigh <- Array.append neigh [|nodes.[(m-1)*kLength+n]|]
                    if m+1 < kLength then
                        neigh <- Array.append neigh [|nodes.[(m+1)*kLength+n]|]
                    let randNeigh = System.Random().Next(0,newKLength) 
                    neigh <- Array.append neigh [|nodes.[randNeigh]|]
                    nodes.[m*kLength+n]<!Initailize(neigh)

           let leader = System.Random().Next(0,newMatrix - 1) 
           if protocol = "gossip" then
               listener<!RecordNumPeople(newMatrix-1)
               listener<!RecordStartTime(System.DateTime.Now.TimeOfDay.Milliseconds)
               printfn "Starting Gossip Protocol IMP-2D"
               nodes.[leader]<!StartGossip("Hello World")
           else if protocol="push-sum" then
               listener<!RecordStartTime(System.DateTime.Now.TimeOfDay.Milliseconds)
               printfn "Starting Push Sum Protocol IMP-2D"
               nodes.[leader]<!StartPushSum(10.0 ** -10.0)
      | _-> ()
System.Console.ReadLine()|>ignore

       
