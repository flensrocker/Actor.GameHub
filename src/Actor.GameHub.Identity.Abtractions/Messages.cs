using System;
using Akka.Actor;

namespace Actor.GameHub.Identity.Abtractions
{
  // ----- UserSessionManager

  public class LoginUserMsg
  {
    public string Username { get; init; } = null!;
    //public string? Password { get; init; }
    //public string? IdToken { get; init; }
  }

  public class UserLoginErrorMsg
  {
    public string ErrorMessage { get; init; } = null!;
  }

  public class UserLoginSuccessMsg
  {
    public Guid UserLoginId { get; } = Guid.NewGuid();
    public IActorRef LoginSender { get; init; } = null!;
    public User User { get; init; } = null!;
  }

  // ----- UserAuthenticator

  public class AuthUserMsg
  {
    public Guid AuthId { get; } = Guid.NewGuid();
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

  public class LoadUserByUsernameMsg
  {
    public Guid LoadId { get; } = Guid.NewGuid();
    public string Username { get; init; } = null!;
  }

  public class UserLoadErrorMsg
  {
    public Guid LoadId { get; init; }
    public string ErrorMessage { get; init; } = null!;
  }

  public class UserLoadSuccessMsg
  {
    public Guid LoadId { get; init; }
    public User User { get; init; } = null!;
  }

  // ----- UserLogin

  public class UserLoginMsg
  {
    public Guid UserLoginId { get; init; }
    public IActorRef UserLogin { get; init; } = null!;
    public User User { get; init; } = null!;
  }

  public class LogoutUserMsg
  {
  }
}
