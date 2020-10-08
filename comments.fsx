






// let a = System.Random(1)

//FULL AND GOSSIP
// if(protocol = "gossip" && topology = "full") then
//     // let finalArr = Array.zeroCreate(numNodes)
//     // for i in [0..numNodes-1] do
//     // finalArr.[i] <- Node listener 10 (i+1) |> spawn system ("Node" + string(i))
//     for i in [0..numNodes-1] do 
//         finalArr.[i] <! Initialize(finalArr)
//     let leader = a.Next(0,numNodes)
//     listener <! RecordNumPeople(numNodes)
//     let time = DateTimeOffset.Now.ToUnixTimeMilliseconds() |> int
//     listener <! RecordStartTime(time)
//     printfn "Starting protocol Gossip!"
//     finalArr.[leader]<! StartGossip("Hello")


// /// FULL AND PUSH-SUM
// if(protocol = "pushsum" && topology = "full") then
//     for i in [0..numNodes-1] do 
//         finalArr.[i] <! Initialize(finalArr)
//     let leader = a.Next(0,numNodes)
//     let time = DateTimeOffset.Now.ToUnixTimeMilliseconds() |> int
//     listener <! RecordStartTime(time)
//     printfn "Starting protocol PushSum!"
//     finalArr.[leader]<! StartPushSum(10.0 ** -10.0)

//let finalList = finalArr |> Array.toList
// printfn "List Length is %i" finalList.Length
// printfn "Array length is %i " finalArr.Length


/// LINE AND PUSH-SUM

// if(protocol= "pushsum" && topology = "line") then
//     printfn "Starting PushSum for line"
//     let mutable  neighbours:IActorRef[]=[||]
//     [ 0 .. numNodes-1]   
//             |> List.iter (fun x ->
//                     let mutable nei: IActorRef list = []
//                     if x = 0 then
//                         nei <- [ finalList.[1] ]
//                     elif x = numNodes-1 then 
//                         nei <- [ finalList.[numNodes-2] ]
//                     else 
//                         nei <- [ finalList.[x-1]; finalList.[x+1] ]

//                     neighbours<-nei |> List.toArray
//                     finalArr.[x]<!Initialize(neighbours)
//                     let leader = a.Next(0,numNodes)
//                     let time = DateTimeOffset.Now.ToUnixTimeMilliseconds() |> int
//                     listener <! RecordStartTime(time)
                    
//                     finalArr.[leader]<!StartPushSum(10.0 ** -10.0)        
//             )


// if(protocol= "gossip" && topology = "line") then
//     printfn "Starting Gossip for line"
//     let mutable  neighbours:IActorRef[]=[||]
//     [ 0 .. numNodes-1]   
//             |> List.iter (fun x ->
//                     let mutable nei: IActorRef list = []
//                     if x = 0 then
//                         nei <- [ finalList.[1] ]
//                     elif x = numNodes-1 then 
//                         nei <- [ finalList.[numNodes-2] ]
//                     else 
//                         nei <- [ finalList.[x-1]; finalList.[x+1] ]

//                     neighbours<-nei |> List.toArray
//                     finalArr.[x]<!Initialize(neighbours)
                 
//             )
//     let leader = a.Next(0,numNodes)
//     listener<! RecordNumPeople(numNodes)
//     let time = DateTimeOffset.Now.ToUnixTimeMilliseconds() |> int
//     listener <! RecordStartTime(time)
//     finalArr.[leader]<!StartGossip("Hello")
    

// nei |> Seq.iter (fun x -> printf "%A \n" x)

    // for i in [0.. numNodes-1] do
    //     // let neigh =[|finalArr.[((i-1+numNodes)%(numNodes+1))];finalArr.[((i+1+numNodes)%(numNodes+1))]|]        
    //     if i = 0 then
    //         neigh <- [finalList.[1]]
    //     elif i = numNodes-1 then
    //         neigh <- [finalList.[numNodes-2]]
    //     else
    //         neigh <-  [ finalList.[i-1] ; finalList.[i+1] ]
            // neigh.[i] <- finalArr.[((i-1+numNodes)%(numNodes))]
        

// neigh |> Seq.iter (fun x -> printf "%A " x)
        // neighbours <- neigh |> List.toArray
            //finalArr.[((i-1+numNodes)%(numNodes))..((i+1+numNodes)%(numNodes))]
        //let neigh:IActorRef[]=finalArr.[((i-1+numNodes)% (numNodes + 1))..((i+1+numNodes)% (numNodes + 1))]
    //     finalArr.[i]<! Initialize(neighbours)
    // let leader = a.Next(0,numNodes)
    // let time = DateTimeOffset.Now.ToUnixTimeMilliseconds() |> int
    // listener <! RecordStartTime(time)
    // printfn "Starting PushSum for line"
    // finalArr.[leader]<!StartPushSum(10.0 ** -10.0)
