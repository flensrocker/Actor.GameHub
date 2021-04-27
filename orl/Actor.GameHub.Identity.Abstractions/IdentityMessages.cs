using System;

namespace Actor.GameHub.Identity.Abstractions
{
  public class RegisterRequest
  {
    public string Name { get; init; }
    public string Password { get; init; }
  }
  public class RegisterResponse
  {
    public Guid PlayerId { get; init; }
  }

  public class SetPlayerIdRequest
  {
    public Guid PlayerId { get; init; }
  }

  public class ChangeNameRequest
  {
    public string NewName { get; init; }
  }

  public class ChangePasswordRequest
  {
    public string OldPassword { get; init; }
    public string NewPassword { get; init; }
  }

  public class PasswordLoginRequest
  {
    public string Password { get; init; }
  }
  public class PasswordLoginResponse
  {
    public Guid PlayerId { get; init; }
    public string Name { get; init; }
    public string AuthToken { get; init; }
  }
}
