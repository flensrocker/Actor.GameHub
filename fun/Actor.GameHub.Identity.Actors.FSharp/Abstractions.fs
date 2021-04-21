module Actor.GameHub.Identity.Actors.Abstractions

open System
open Actor.GameHub.Identity.Abstractions

type IdentityInternalMessage =
    | UserAuthErrorMsg of AuthId: Guid * ErrorMessage: string
    | UserAuthSuccessMsg of AuthId: Guid * User: User
    | AuthTerminated of AuthId: Guid

type AuthUserData =
    { Username: string (*; Password: string; IdToken: string*)  }

type AuthenticatorMessage =
    | AuthUserMsg of AuthId: Guid * AuthData: AuthUserData
    | UserLoadForAuthErrorMsg of LoadId: Guid * ErrorMessage: string
    | UserLoadForAuthSuccessMsg of LoadId: Guid * User: UserForAuth
    | LoaderTerminated of LoadId: Guid

type LoaderMessage = LoadUserByUsernameForAuthMsg of LoadId: Guid * Username: string
