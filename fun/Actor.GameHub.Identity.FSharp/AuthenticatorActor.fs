module Actor.GameHub.Identity.AuthenticatorActor

open System
open Akka.FSharp
open Akka.Actor

open Actor.GameHub.Identity.Abstractions

type AuthLoaderData =
    { AuthId: Guid
      AuthData: AuthUserData
      AuthOrigin: IActorRef }

type AuthState =
    { AuthLoaderDataByLoadId: Map<Guid, AuthLoaderData> }

let initialAuthState = { AuthLoaderDataByLoadId = Map.empty }

let addLoader loadId authId authData authOrigin state =
    { state with
          AuthLoaderDataByLoadId =
              Map.add
                  loadId
                  { AuthId = authId
                    AuthData = authData
                    AuthOrigin = authOrigin }
                  state.AuthLoaderDataByLoadId }

let removeLoader loadId state =
    { state with
          AuthLoaderDataByLoadId = Map.remove loadId state.AuthLoaderDataByLoadId }

let handleAuth spawnLoader authId (authData: AuthUserData) (mailbox: Actor<_>) state =
    let authOrigin = mailbox.Sender()
    let loadId = Guid.NewGuid()

    match Map.containsKey loadId state.AuthLoaderDataByLoadId with
    | true ->
        authOrigin
        <! UserAuthErrorMsg(authId, "user auth loadId error, try again...")

        state
    | false ->
        let loadUserMsg =
            LoadUserByUsernameForAuthMsg(loadId, authData.Username)

        let loaderRef = spawnLoader mailbox.Context loadId

        mailbox.Context.WatchWith(loaderRef, LoaderTerminated loadId)
        <! loadUserMsg

        addLoader loadId authId authData authOrigin state

let handleLoadError loadId errorMessage (mailbox: Actor<_>) state =
    let loaderRef = mailbox.Sender()

    match Map.tryFind loadId state.AuthLoaderDataByLoadId with
    | None -> state
    | Some authOrigin ->
        authOrigin.AuthOrigin
        <! UserAuthErrorMsg(authOrigin.AuthId, errorMessage)

        mailbox.Context.Stop(loaderRef)
        removeLoader loadId state

let handleLoadSuccess loadId user (mailbox: Actor<_>) state =
    let loaderRef = mailbox.Sender()

    match Map.tryFind loadId state.AuthLoaderDataByLoadId with
    | None -> state
    | Some authData ->
        // TODO do authentication based on password/idToken
        authData.AuthOrigin
        <! UserAuthSuccessMsg(authData.AuthId, user)

        mailbox.Context.Stop(loaderRef)
        removeLoader loadId state

let handleLoaderTerminated loadId state =
    match Map.tryFind loadId state.AuthLoaderDataByLoadId with
    | None -> state
    | Some authOrigin ->
        authOrigin.AuthOrigin
        <! UserAuthErrorMsg(authOrigin.AuthId, $"unexpected stop of loader {loadId}")

        removeLoader loadId state

let authenticator allowedAuthId spawnLoader (mailbox: Actor<_>) =
    let rec loop state =
        actor {
            let! msg = mailbox.Receive()

            let nextState =
                match msg with
                | AuthUserMsg (authId, authData) when authId = allowedAuthId ->
                    handleAuth spawnLoader authId authData mailbox
                | UserLoadForAuthErrorMsg (loadId, errorMessage) -> handleLoadError loadId errorMessage mailbox
                | UserLoadForAuthSuccessMsg (loadId, user) ->
                    handleLoadSuccess
                        loadId
                        { UserId = user.UserId
                          Username = user.Username }
                        mailbox
                | LoaderTerminated (loadId) -> handleLoaderTerminated loadId
                | _ -> id

            return! loop (nextState state)
        }

    loop initialAuthState

let spawnAuthenticator spawnLoader parent authId =
    spawnOpt parent (IdentityMetadata.AuthenticatorName authId) (authenticator authId spawnLoader) [ stoppingStrategy ]
