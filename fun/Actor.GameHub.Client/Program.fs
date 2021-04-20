open System
open System.IO
open Akka.FSharp
open Akka.Cluster.Tools.Client
open Actor.GameHub.Identity.Abstractions

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
        | _ -> "gamehub-client.akka"

    use gameHubClientSystem =
        configFilename
        |> getConfig
        |> System.create "GameHubClient"

    let clusterClient =
        gameHubClientSystem.ActorOf(ClusterClient.Props(ClusterClientSettings.Create(gameHubClientSystem)))

    async {
        let! response =
            clusterClient
            <? new ClusterClient.Send(IdentityMetadata.IdentityPath, LoginUserMsg(Guid.NewGuid(), "lars"))

        match response with
        | UserLoginErrorMsg (loginId, errorMessage) -> printfn "error: %s" errorMessage
        | UserLoginSuccessMsg (loginId, user) -> printfn "logged in: %s" user.Username
        | _ -> printfn "unknown response"
        |> ignore
    }
    |> Async.RunSynchronously

    gameHubClientSystem.Terminate()
    |> Async.AwaitTask
    |> Async.RunSynchronously

    0
