#time "on"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"
open Akka.Actor
open Akka.FSharp
open System.Diagnostics
open System.Threading
let system = ActorSystem.Create("Rumour")

type Message = 
    | NeighBour of list<IActorRef>
    | Rumour of string


let numNodes = 10






































let childActors (mailbox: Actor<_>) =
    let a = System.Random()
    let rec loop (rumour: string)(neighbourList: list<IActorRef>)(counter: int) =
        actor{
            let! message = mailbox.Receive()
            match message with 
            | Rumour(rumour)->
                let leader = a.Next(neighbourList.Length)
                neighbourList.[leader] <! rumour
                if(counter + 1 = numNodes) then
                   neighbourList.[leader] <! "Recived Rumour"
                   return! loop rumour neighbourList counter
                elif (counter >= numNodes) then
                    return! loop rumour neighbourList counter

                else
                    return! loop rumour neighbourList (counter+1)
            | NeighBour(neighbourList) ->
                return! loop rumour neighbourList counter

        }
    loop "" [] 0


system.Terminate()