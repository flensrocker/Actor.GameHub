# Actor Model

```mermaid
graph TD
  user --> Identity
  Identity --> UserSessionManager
  UserSessionManager --> UserSession-UserId1
  UserSessionManager --> UserSession-UserId2
  UserSessionManager --> UserAuthenticator-Id1
  UserAuthenticator-Id1 --> UserLoader-Id1
  UserSession-UserId1 --> UserLogin-Id1
  UserSession-UserId1 --> UserLogin-Id2

  user --> TerminalManager
  TerminalManager --> Terminal-Id1
  TerminalManager --> Terminal-Id2

  user --> GameHub
```

# Messages

## Identity

- LoginUser => UserSessionManager

## UserSessionManager

- LoginUser
  - AuthUser -> new UserAuthenticator
- UserAuthError
  - UserLoginError -> Sender
- UserAuthSuccess
  - UserLoginSuccess -> UserSession-{userId}

## UserAuthenticator-{id}

- AuthUser
  - LoadUserByUsername -> new UserLoader
- UserLoadError
  - UserAuthError -> Parent
- UserLoadSuccess
  - UserAuthError -> Sender => Stop
  - UserAuthSuccess -> Sender => Stop

## UserLoader-{id}

- LoadUserByUsername
  - UserLoadError -> Sender => Stop
  - UserLoadSuccess -> Sender => Stop

## UserSession-{userId}

- UserLoginSuccess -> new UserLogin
- LogoutUser

## UserLogin-{id}

- UserLoginSuccess
  - UserLogin -> LoginSender
- LogoutUser => Parent

## Terminal-{id}

- Input
  - Output
