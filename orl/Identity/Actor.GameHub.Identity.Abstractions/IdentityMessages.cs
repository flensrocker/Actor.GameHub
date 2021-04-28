using System;

namespace Actor.GameHub.Identity.Abstractions
{
  [Serializable]
  public class RegisterRequest
  {
    public string Username { get; init; }
    public string Password { get; init; }
  }

  [Serializable]
  public class RegisterResponse
  {
    public Guid PlayerId { get; init; }
  }

  [Serializable]
  public class SetPlayerIdRequest
  {
    public Guid PlayerId { get; init; }
  }

  [Serializable]
  public class ChangeUsernameRequest
  {
    public string NewUsername { get; init; }
  }

  [Serializable]
  public class ChangePasswordRequest
  {
    public string OldPassword { get; init; }
    public string NewPassword { get; init; }
  }

  [Serializable]
  public class PasswordLoginRequest
  {
    public string Username { get; init; }
    public string Password { get; init; }
  }

  [Serializable]
  public class PasswordLoginResponse
  {
    public Guid PlayerId { get; init; }
    public string Username { get; init; }
    public string AuthToken { get; init; }
  }
}
