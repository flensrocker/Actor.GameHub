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

type LoginUserMsg = { UserLoginId: Guid; Username: string }

type UserLoginErrorMsg =
    { UserLoginId: Guid
      ErrorMessage: string }

type UserLoginSuccessMsg = { UserLoginId: Guid; User: User }
