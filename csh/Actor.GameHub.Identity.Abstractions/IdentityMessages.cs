using System;

namespace Actor.GameHub.Identity.Abstractions
{
  // ----- Identity

  public class LoginUserMsg
  {
    public Guid UserLoginId { get; init; }
    public string Username { get; init; } = null!;
    //public string? Password { get; init; }
    //public string? IdToken { get; init; }
  }

  public class UserLoginErrorMsg
  {
    public Guid UserLoginId { get; init; }
    public string ErrorMessage { get; init; } = null!;
  }

  public class UserLoginSuccessMsg
  {
    public Guid UserLoginId { get; init; }
    public User User { get; init; } = null!;
  }

  // ----- UserAuthenticator

  public class AuthUserMsg
  {
    public Guid AuthId { get; init; }
    public LoginUserMsg LoginUserMsg { get; init; } = null!;
  }

  public class UserAuthErrorMsg
  {
    public Guid AuthId { get; init; }
    public string ErrorMessage { get; init; } = null!;
  }

  public class UserAuthSuccessMsg
  {
    public Guid AuthId { get; init; }
    public User User { get; init; } = null!;
    //public string AuthToken { get; init; } = null!;
  }

  // ----- UserLoader

  public class LoadUserByUsernameForAuthMsg
  {
    public Guid LoadId { get; init; }
    public string Username { get; init; } = null!;
  }

  public class UserLoadForAuthErrorMsg
  {
    public Guid LoadId { get; init; }
    public string ErrorMessage { get; init; } = null!;
  }

  public class UserLoadForAuthSuccessMsg
  {
    public Guid LoadId { get; init; }
    public UserForAuth User { get; init; } = null!;
  }
}
