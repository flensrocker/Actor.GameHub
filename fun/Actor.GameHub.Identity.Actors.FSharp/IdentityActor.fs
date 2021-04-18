﻿module Actor.GameHub.Identity.Actors.IdentityActor

open System
open Akka.FSharp
open Akka.Actor

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
        <! UserLoginErrorMsg(loginId, "user loginId error, try again...")

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
        <! UserLoginSuccessMsg(loginData.LoginId, user)

        authRef |> mailbox.Unwatch |> mailbox.Context.Stop
        removeAuth authId state

let handleAuthTerminated authId state =
    match Map.tryFind authId state.AuthDataByAuthId with
    | None -> state
    | Some authData ->
        authData.LoginOrigin
        <! UserLoginErrorMsg(authData.LoginId, $"unexpected stop of authenticator {authId}")

        removeAuth authId state

let identity spawnAuthenticator (mailbox: Actor<_>) =
    let rec loop state =
        actor {
            let! msg = mailbox.Receive()

            let nextState =
                match msg with
                | UserAuthErrorMsg (authId, errorMessage) -> handleAuthError authId errorMessage mailbox
                | UserAuthSuccessMsg (authId, user) -> handleAuthSuccess authId user mailbox
                | AuthTerminated (authId) -> handleAuthTerminated authId
                | IdentityPublicMessage pubMsg ->
                    match pubMsg with
                    | LoginUserMsg (loginId, username) -> handleLogin spawnAuthenticator loginId username mailbox

            return! loop (nextState state)
        }

    loop initialIdentityState

let spawnIdentity spawnAuthenticator system =
    spawn system IdentityMetadata.IdentityName (identity spawnAuthenticator)