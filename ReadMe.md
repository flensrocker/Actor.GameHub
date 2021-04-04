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

## Terminal

- OpenTerminal
  - LoginTerminal -> new TerminalSession

## TerminalSession-{id}

- LoginTerminal
  - LoginUser
- UserLoginError
  - TerminalAddError => Stop
- UserLoginSuccess
  - TerminalAddSuccess
- InputTerminal
  - TerminalOutput
- CloseTerminal => Stop
