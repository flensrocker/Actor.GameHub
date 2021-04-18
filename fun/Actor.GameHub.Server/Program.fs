open System
open System.IO
open Akka.FSharp

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
        configFilename |> getConfig |> System.create "GameHub"

    Console.ReadLine() |> ignore

    gameHubServerSystem.Terminate()
    |> Async.AwaitTask
    |> Async.RunSynchronously

    0
