open System
open System.IO
open Akka.FSharp
open Actor.GameHub.Identity.Abstractions
open Actor.GameHub.Identity.Actors
open Actor.GameHub.Identity.EntityFrameworkCore
open Akka.Cluster.Tools.Client
open System.Collections.Immutable
open Akka.Actor

let getConfig filename =
    match File.Exists filename with
    | false -> Configuration.defaultConfig ()
    | true ->
        filename
        |> File.ReadAllText
        |> Configuration.parse

[<EntryPoint>]
let main argv =
    let configFilename =
        match Array.toList argv with
        | arg0 :: _ -> arg0
        | _ -> "gamehub-seed.akka"

    use gameHubServerSystem =
        configFilename
        |> getConfig
        |> System.create "GameHub"
        |> IdentityActor.addIdentityActors
            (fun () -> IdentityDbRepository.newIdentityDbRepository Actor.GameHub.Configuration.dbOptions)

    //async {
    //    let clientSystem =
    //        "gamehub-client.akka"
    //        |> getConfig
    //        |> System.create "GameHubClient"

    //    let clusterClient =
    //        clientSystem.ActorOf(ClusterClient.Props(ClusterClientSettings.Create(clientSystem)))

    //    printfn "asking..."

    //    let! response =
    //        clusterClient
    //        <? new ClusterClient.Send(IdentityMetadata.IdentityPath, LoginUserMsg(Guid.NewGuid(), "lars"))

    //    printfn "answered"

    //    match response with
    //    | UserLoginErrorMsg (loginId, errorMessage) -> printfn "error: %s" errorMessage
    //    | UserLoginSuccessMsg (loginId, user) -> printfn "logged in: %s" user.Username
    //    | _ -> printfn "unknown response"
    //    |> ignore
    //}
    //|> Async.Start

    Console.ReadLine() |> ignore

    gameHubServerSystem.Terminate()
    |> Async.AwaitTask
    |> Async.RunSynchronously

    0
