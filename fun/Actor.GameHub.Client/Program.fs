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

    async {
        let clusterClient =
            gameHubClientSystem.ActorOf(ClusterClient.Props(ClusterClientSettings.Create(gameHubClientSystem)))

        printfn "send request"

        let! response =
            clusterClient
            <? new ClusterClient.Send(
                IdentityMetadata.IdentityPath,
                { UserLoginId = Guid.NewGuid()
                  Username = "lars" }
            )

        printfn "got reply"

        match box response with
        | :? UserLoginErrorMsg as errorMsg -> printfn "error: %s" errorMsg.ErrorMessage
        | :? UserLoginSuccessMsg as successMsg -> printfn "logged in: %s" successMsg.User.Username
        | _ -> printfn "unknown response"
        |> ignore
    }
    |> Async.Start

    printfn "waiting..."
    let input = Console.ReadLine()

    gameHubClientSystem.Terminate()
    |> Async.AwaitTask
    |> Async.RunSynchronously

    0
