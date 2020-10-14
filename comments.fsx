






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






// let mutable actorList: IActorRef list = []

// let k = 2

// let array2D =  Array2D.create k k 0

// actorList <- array2D |> Seq.cast<int> |> Seq.toList

// printfn "%A" actorList
// l |> List.iter (fun x -> printf "%d " x)
// for i in [0..k-1] do
//     for j in [0..k-1] do
//         printfn "Value is %i" Array2D.get array2d i j

// let k = 2
// let array : IActorRef [,] = Array2D.zeroCreate k k

// let toListList arr = 
//     [for x in 0 .. Array2D.length1 arr - 1 ->
//         [ for y in 0 .. Array2D.length2 arr - 1 -> arr.[x, y] ]
//     ]


// let arrayList = toListList array


let arr = Array2D.zeroCreate 2 2 
for i = 0 to 1 do  
    for j = 0 to 1 do  
        printf "%d " arr.[i,j]  
    printf "\n"  