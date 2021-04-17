module Actor.GameHub.Identity.LoaderActor

open Akka.FSharp

open Actor.GameHub.Identity.Abstractions

let loadUserForAuth (identityRepository: IIdentityRepository) loadOrigin loadId username =
    let user =
        identityRepository.FindUserByUsernameForAuth username

    loadOrigin
    <! match user with
       | Some userForAuth -> UserLoadForAuthSuccessMsg(loadId, userForAuth)
       | None -> UserLoadForAuthErrorMsg(loadId, "user not found")

let loader allowedLoadId identityRepository (mailbox: Actor<_>) =
    let rec loop () =
        actor {
            let! msg = mailbox.Receive()

            match msg with
            | LoadUserByUsernameForAuthMsg (loadId, username) when loadId = allowedLoadId ->
                loadUserForAuth identityRepository (mailbox.Sender()) loadId username
            | _ -> ()

            return! loop ()
        }

    loop ()

let spawnLoader getIdentityRepository parent loadId =
    spawnOpt parent (IdentityMetadata.LoaderName loadId) (loader loadId (getIdentityRepository ())) [ stoppingStrategy ]
