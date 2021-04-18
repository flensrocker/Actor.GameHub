module Actor.GameHub.Identity.Actors.AuthenticatorActor

open System
open Akka.FSharp
open Akka.Actor

open Actor.GameHub.Extensions
open Actor.GameHub.Identity.Abstractions
open Actor.GameHub.Identity.Actors.Abstractions

type LoaderData =
    { AuthId: Guid
      AuthData: AuthUserData
      AuthOrigin: IActorRef }

type AuthState =
    { LoaderDataByLoadId: Map<Guid, LoaderData> }

let initialAuthState = { LoaderDataByLoadId = Map.empty }

let addLoader loadId authId authData authOrigin state =
    { state with
          LoaderDataByLoadId =
              Map.add
                  loadId
                  { AuthId = authId
                    AuthData = authData
                    AuthOrigin = authOrigin }
                  state.LoaderDataByLoadId }

let removeLoader loadId state =
    { state with
          LoaderDataByLoadId = Map.remove loadId state.LoaderDataByLoadId }

let handleAuth spawnLoader authId (authData: AuthUserData) (mailbox: Actor<_>) state =
    let authOrigin = mailbox.Sender()
    let loadId = Guid.NewGuid()

    match Map.containsKey loadId state.LoaderDataByLoadId with
    | true ->
        authOrigin
        <! UserAuthErrorMsg(authId, "user auth loadId error, try again...")

        state
    | false ->
        spawnLoader mailbox.Context loadId
        |> monitorWith (LoaderTerminated loadId) mailbox
        <! LoadUserByUsernameForAuthMsg(loadId, authData.Username)

        addLoader loadId authId authData authOrigin state

let handleLoadError loadId errorMessage (mailbox: Actor<_>) state =
    let loaderRef = mailbox.Sender()

    match Map.tryFind loadId state.LoaderDataByLoadId with
    | None -> state
    | Some authData ->
        authData.AuthOrigin
        <! UserAuthErrorMsg(authData.AuthId, errorMessage)

        loaderRef
        |> mailbox.Unwatch
        |> mailbox.Context.Stop

        removeLoader loadId state

let handleLoadSuccess loadId user (mailbox: Actor<_>) state =
    let loaderRef = mailbox.Sender()

    match Map.tryFind loadId state.LoaderDataByLoadId with
    | None -> state
    | Some authData ->
        // TODO do authentication based on password/idToken
        authData.AuthOrigin
        <! UserAuthSuccessMsg(authData.AuthId, user)

        loaderRef
        |> mailbox.Unwatch
        |> mailbox.Context.Stop

        removeLoader loadId state

let handleLoaderTerminated loadId state =
    match Map.tryFind loadId state.LoaderDataByLoadId with
    | None -> state
    | Some authData ->
        authData.AuthOrigin
        <! UserAuthErrorMsg(authData.AuthId, $"unexpected stop of loader {loadId}")

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
