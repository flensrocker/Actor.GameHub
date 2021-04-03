# Actor GameHub

## Actor Model

```mermaid
graph TD
  user --> Identity
  Identity --> UserSessionManager
  UserSessionManager --> UserSession-UserId1
  UserSessionManager --> UserSession-UserId2
  UserSessionManager --> UserLoader-Id1
  UserSessionManager --> UserAuthenticator-Id1
  UserSession-UserId1 --> UserLogin-Id1
  UserSession-UserId1 --> UserLogin-Id2

  user --> TerminalManager
  TerminalManager --> Terminal-Id1
  TerminalManager --> Terminal-Id2

  user --> GameHub
```

## Messages

### Identity

- UserLogin => UserSessionManager

### UserSessionManager

- UserLogin
  - LoadUser -> UserLoader
- LoadUserError
  - UserLoginError -> Sender
- LoadUserSuccess
  - AuthUser -> UserAuthenticator
- AuthUserError
  - UserLoginError -> Sender
- AuthUserSuccess
  - UserLoginSuccess -> UserSession-{userId}
- SessionClose

### UserLoader-{id}

- LoadUser
  - LoadUserError -> Sender => Stop
  - LoadUserSuccess -> Sender => Stop

### UserAuthenticator-{id}

- AuthUser
  - AuthUserError -> Sender => Stop
  - AuthUserSuccess -> Sender => Stop

### UserSession-{userId}

- UserLoginSuccess
- UserLogout
  - SessionClose -> Parent

### UserLogin-{id}

- UserLogout => Parent

### Terminal-{id}

- Input
  - Output
