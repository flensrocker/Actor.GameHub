module Actor.GameHub.Identity.IdentityActor

open Akka.FSharp
open Akka.Actor

open Actor.GameHub.Identity.Abstractions

let handleIdentity (mailbox: Actor<_>) msg =
    let handleLogin loginId username = ()

    let handleAuthError authId errorMessage = ()

    let handleAuthSuccess authId user = ()

    let handleTerminated terminatedMsg = ()

    match msg with
    | LoginUserMsg (loginId, username) -> handleLogin loginId username
    | UserAuthErrorMsg (authId, errorMessage) -> handleAuthError authId errorMessage
    | UserAuthSuccessMsg (authId, user) -> handleAuthSuccess authId user

let spawnIdentity system =
    spawn system IdentityMetadata.IdentityName (actorOf2 handleIdentity)
