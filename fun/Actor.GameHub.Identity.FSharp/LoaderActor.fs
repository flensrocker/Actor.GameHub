module Actor.GameHub.Identity.LoaderActor

open Akka.FSharp

open Actor.GameHub.Identity.Abstractions

let handleLoader (mailbox: Actor<_>) msg =
    let loadUserForAuth loadId user = ()

    match msg with
    | LoadUserByUsernameForAuthMsg (loadId, username) -> loadUserForAuth loadId username

let spawnLoader parent loadId =
    spawnOpt parent (IdentityMetadata.LoaderName loadId) (actorOf2 handleLoader) [stoppingStrategy]
