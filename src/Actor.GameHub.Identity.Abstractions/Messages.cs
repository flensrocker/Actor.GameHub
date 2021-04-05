using System;
using Akka.Actor;

namespace Actor.GameHub.Identity.Abstractions
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

  public class AddUserLoginMsg
  {
    public Guid UserLoginId { get; init; }
    public IActorRef LoginOrigin { get; init; } = null!;
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

  public class UserLoadErrorMsg
  {
    public Guid LoadId { get; init; }
    public string ErrorMessage { get; init; } = null!;
  }

  public class UserLoadForAuthSuccessMsg
  {
    public Guid LoadId { get; init; }
    public User User { get; init; } = null!;
  }

  // ----- Shell

  public class UserLoginSuccessMsg
  {
    public Guid UserLoginId { get; init; }
    public IActorRef ShellRef { get; init; } = null!;
    public User User { get; init; } = null!;
  }

  public class InputCommandMsg
  {
    public Guid InputCommandId { get; init; }
    public string Command { get; init; } = null!;
    public string? Parameter { get; init; }
  }

  public class InputErrorMsg
  {
    public Guid InputCommandId { get; init; }
    public string ErrorMessage { get; init; } = null!;
  }

  public class InputSuccessMsg
  {
    public Guid InputCommandId { get; init; }
    public string Output { get; init; } = null!;
  }

  public class LogoutUserMsg
  {
  }

  // ----- ShellCommand

  public class ExecuteCommandMsg
  {
    public Guid CommandId { get; init; }
    public InputCommandMsg Input { get; init; } = null!;
  }

  public class CommandErrorMsg
  {
    public Guid CommandId { get; init; }
    public string ErrorMessage { get; init; } = null!;
  }

  public class CommandSuccessMsg
  {
    public Guid CommandId { get; init; }
    public string Output { get; init; } = null!;
  }
}
