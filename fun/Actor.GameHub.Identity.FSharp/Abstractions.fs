module Actor.GameHub.Identity.Abstractions

open System

module IdentityMetadata =
    let IdentityName = "Identity"
    let IdentityPath = $"/user/{IdentityName}"
    let UserAuthenticatorName authId = $"UserAuthenticator-{authId}"
    let UserLoaderName loadId = $"UserLoader-{loadId}"

type User = { UserId: Guid; Username: string }

type UserForAuth = { UserId: Guid; Username: string }

type IdentityMessage =
    | LoginUserMsg of UserLoginId: Guid * Username: string
    | UserLoginErrorMsg of UserLoginId: Guid * ErrorMessage: string
    | UserLoginSuccessMsg of UserLoginId: Guid * User: User

type AuthenticatorMessage =
    | AuthUserMsg of AuthId: Guid * Username: string
    | UserAuthErrorMsg of AuthId: Guid * ErrorMessage: string
    | UserAuthSuccessMsg of AuthId: Guid * User: User

type LoaderMessage =
    | LoadUserByUsernameForAuthMsg of LoadId: Guid * Username: string
    | UserLoadForAuthErrorMsg of LoadId: Guid * ErrorMessage: string
    | UserLoadForAuthSuccessMsg of LoadId: Guid * User: UserForAuth
