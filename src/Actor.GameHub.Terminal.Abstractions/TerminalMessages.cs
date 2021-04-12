using System;
using Actor.GameHub.Identity.Abstractions;
using Akka.Actor;

namespace Actor.GameHub.Terminal.Abstractions
{
  // ----- Terminal

  public class OpenTerminalMsg
  {
    public string Username { get; init; } = null!;
  }

  // ----- TerminalSession

  public class LoginTerminalMsg
  {
    public Guid TerminalId { get; init; }
    public LoginUserMsg LoginUser { get; init; } = null!;
  }

  public class TerminalOpenErrorMsg
  {
    public string ErrorMessage { get; init; } = null!;
  }

  public class TerminalOpenSuccessMsg
  {
    public Guid TerminalId { get; init; }
    public IActorRef TerminalRef { get; init; } = null!;
    public Guid UserId { get; init; }
    public string Username { get; init; } = null!;
    //public string? AuthToken { get; init; }
  }

  public class InputTerminalMsg
  {
    public Guid TerminalId { get; init; }
    public Guid TerminalInputId { get; init; }
    public string Command { get; init; } = null!;
    public string? Parameter { get; init; }
  }

  public class TerminalInputErrorMsg
  {
    public Guid TerminalId { get; init; }
    public Guid TerminalInputId { get; init; }
    public int ExitCode { get; init; }
    public string ErrorMessage { get; init; } = null!;
  }

  public class TerminalInputSuccessMsg
  {
    public Guid TerminalId { get; init; }
    public Guid TerminalInputId { get; init; }
    public string Output { get; init; } = null!;
  }

  public class CloseTerminalMsg
  {
    public Guid TerminalId { get; init; }
  }

  public class TerminalClosedMsg
  {
    public Guid TerminalId { get; init; }
    public Guid TerminalInputId { get; init; }
    public int ExitCode { get; set; }
  }
}
