#time "on"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"

open Akka.Actor
open Akka.FSharp
open System.Diagnostics
open System




let system = ActorSystem.Create("Fsharp")

let topology = "full"
let protocol = "gossip"

type Message = 
    | HeardRumour 
    | Wakeup 
    | Converged
    | Value of double*double
    | Dead of IActorRef 
    | Int32 of int


 let Timer (mailbox: Actor<_>) = 
    // let mutable counter = 0 
    // let mutable numNodes = 0 
    // let mutable startime = 0 |> double
    // let mutable endTime = 0 |> double

    let rec loop (counter : int)(numNodes: int)(n:int) = actor {
        let! message = mailbox.Receive ()
        match message with
            | HeardRumour->
                if(counter + 1 <= numNodes) then 
                    return! loop (counter+1) numNodes n
                elif(counter >= numNodes)  then
                    return! loop counter numNodes n
                else 
                    return! loop (counter+1) numNodes n
                    // endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() |> double
                    // printfn "Time take to Converge Gossip is %f" (endTime - startime)
                    // Environment.Exit 0

                

            |  Converged -> 
                if(counter = numNodes) then
                    return! loop (counter) numNodes n
                    // endTime <- DateTimeOffset.Now.ToUnixTimeMilliseconds() |> double
                    // printfn "Time take to Converge push-sum is %f" (endTime - startime)
                    // Environment.Exit 0
                 // let endTime = system.currenttime
                

            | Int32(n) ->
                if(numNodes + 1 = n) then
                    return! loop counter (numNodes) (n+1)
                elif (numNodes >= n) then 
                    return! loop (counter) (numNodes) n
                else 
                    return! loop (counter) (numNodes) (n+1)
                
                // numNodes <- n
                // startime <- DateTimeOffset.Now.ToUnixTimeMilliseconds() |> double
                // printfn "Numnodes are %i" n

                
            | _ -> 
                printfn "Invalid Message"
                Environment.Exit 1
           



        return! loop counter numNodes n
    }
    loop 0 0 0



let timer = spawn system "timer" Timer



// let timer = system.ActorOf(Props(typedefof<Timer>, Array.empty))