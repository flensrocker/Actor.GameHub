module Actor.GameHub.Identity.Actors.IdentityActor

open System
open Akka.FSharp
open Akka.Actor
open Akka.Cluster.Tools.PublishSubscribe
open Akka.Cluster.Tools.Client

open Actor.GameHub.Extensions
open Actor.GameHub.Identity.Abstractions
open Actor.GameHub.Identity.Actors.Abstractions

type AuthData =
    { LoginId: Guid
      LoginOrigin: IActorRef }

type IdentityState =
    { AuthDataByAuthId: Map<Guid, AuthData> }

let initialIdentityState = { AuthDataByAuthId = Map.empty }

let addAuth authId loginId loginOrigin state =
    { state with
          AuthDataByAuthId =
              Map.add
                  authId
                  { LoginId = loginId
                    LoginOrigin = loginOrigin }
                  state.AuthDataByAuthId }

let removeAuth authId state =
    { state with
          AuthDataByAuthId = Map.remove authId state.AuthDataByAuthId }

let handleLogin spawnAuthenticator loginId username (mailbox: Actor<_>) state =
    let loginOrigin = mailbox.Sender()
    let authId = Guid.NewGuid()

    match Map.containsKey authId state.AuthDataByAuthId with
    | true ->
        loginOrigin
        <! { UserLoginId = loginId
             ErrorMessage = "user loginId error, try again..." }

        state
    | false ->
        spawnAuthenticator mailbox authId
        |> monitorWith (AuthTerminated authId) mailbox.Context
        <! AuthUserMsg(authId, { Username = username })

        addAuth authId loginId loginOrigin state

let handleAuthError authId errorMessage (mailbox: Actor<_>) state =
    let authRef = mailbox.Sender()

    match Map.tryFind authId state.AuthDataByAuthId with
    | None -> state
    | Some loginData ->
        loginData.LoginOrigin
        <! UserAuthErrorMsg(loginData.LoginId, errorMessage)

        authRef |> mailbox.Unwatch |> mailbox.Context.Stop
        removeAuth authId state

let handleAuthSuccess authId user (mailbox: Actor<_>) state =
    let authRef = mailbox.Sender()

    match Map.tryFind authId state.AuthDataByAuthId with
    | None -> state
    | Some loginData ->
        loginData.LoginOrigin
        <! { UserLoginId = loginData.LoginId
             User = user }

        authRef |> mailbox.Unwatch |> mailbox.Context.Stop
        removeAuth authId state

let handleAuthTerminated authId state =
    match Map.tryFind authId state.AuthDataByAuthId with
    | None -> state
    | Some authData ->
        authData.LoginOrigin
        <! { UserLoginId = authData.LoginId
             ErrorMessage = $"unexpected stop of authenticator {authId}" }

        removeAuth authId state

let identity spawnAuthenticator (mailbox: Actor<_>) =
    let rec loop state =
        actor {
            let! msg = mailbox.Receive()

            let nextState =
                match box msg with
                | :? IdentityInternalMessage as internMsg ->
                    match internMsg with
                    | UserAuthErrorMsg (authId, errorMessage) -> handleAuthError authId errorMessage mailbox
                    | UserAuthSuccessMsg (authId, user) -> handleAuthSuccess authId user mailbox
                    | AuthTerminated (authId) -> handleAuthTerminated authId
                | :? LoginUserMsg as loginMsg ->
                    handleLogin spawnAuthenticator loginMsg.UserLoginId loginMsg.Username mailbox
                | _ -> id

            return! loop (nextState state)
        }

    logInfo mailbox "==> Identity started"
    loop initialIdentityState

let spawnIdentity spawnAuthenticator system =
    let identityRef =
        spawn system IdentityMetadata.IdentityName (identity spawnAuthenticator)

    DistributedPubSub.Get(system).Mediator
    <! (new Put(identityRef))

    ClusterClientReceptionist
        .Get(system)
        .RegisterService(identityRef)

    system

let addIdentityActors newIdentityRepository system =
    let spawnLoader =
        LoaderActor.spawnLoader newIdentityRepository

    let spawnAuthenticator =
        AuthenticatorActor.spawnAuthenticator spawnLoader

    spawnIdentity spawnAuthenticator system
