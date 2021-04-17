module Actor.GameHub.Identity.Abstractions

open System
open Akka.Actor
open Akka.FSharp

module IdentityMetadata =
    let IdentityName = "Identity"
    let IdentityPath = $"/user/{IdentityName}"
    let AuthenticatorName authId = $"Authenticator-{authId}"
    let LoaderName loadId = $"Loader-{loadId}"

type User = { UserId: Guid; Username: string }

type UserForAuth = { UserId: Guid; Username: string }

type IIdentityRepository =
    abstract member FindUserByUsernameForAuth : username: string -> UserForAuth option

type IdentityMessage =
    | LoginUserMsg of UserLoginId: Guid * Username: string
    | UserAuthErrorMsg of AuthId: Guid * ErrorMessage: string
    | UserAuthSuccessMsg of AuthId: Guid * User: User
    | AuthTerminated of AuthId: Guid

type IdentityReply =
    | UserLoginErrorMsg of UserLoginId: Guid * ErrorMessage: string
    | UserLoginSuccessMsg of UserLoginId: Guid * User: User

type AuthUserData =
    { Username: string (*; Password: string; IdToken: string*)  }

type AuthenticatorMessage =
    | AuthUserMsg of AuthId: Guid * AuthData: AuthUserData
    | UserLoadForAuthErrorMsg of LoadId: Guid * ErrorMessage: string
    | UserLoadForAuthSuccessMsg of LoadId: Guid * User: UserForAuth
    | LoaderTerminated of LoadId: Guid

type LoaderMessage = LoadUserByUsernameForAuthMsg of LoadId: Guid * Username: string

let stoppingStrategy =
    SpawnOption.SupervisorStrategy((new StoppingSupervisorStrategy()).Create())
