module Actor.GameHub.Identity.Abstractions

open System

module IdentityMetadata =
    let IdentityName = "Identity"
    let IdentityPath = $"/user/{IdentityName}"
    let AuthenticatorName authId = $"Authenticator-{authId}"
    let LoaderName loadId = $"Loader-{loadId}"

type User = { UserId: Guid; Username: string }

type UserForAuth = { UserId: Guid; Username: string }

type IIdentityRepository =
    abstract member FindUserByUsernameForAuth : username: string -> UserForAuth option

type IdentityPublicMessage = LoginUserMsg of UserLoginId: Guid * Username: string

type IdentityPublicReply =
    | UserLoginErrorMsg of UserLoginId: Guid * ErrorMessage: string
    | UserLoginSuccessMsg of UserLoginId: Guid * User: User
