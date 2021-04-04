using System;
using Actor.GameHub.Identity.Abstractions;
using Akka.Actor;

namespace Actor.GameHub.Terminal.Abstractions
{
  // ----- Terminal

  public class OpenTerminalMsg
  {
    public LoginUserMsg LoginUser { get; init; } = null!;
  }

  // ----- TerminalSession

  public class LoginTerminalMsg
  {
    public Guid TerminalId { get; } = Guid.NewGuid();
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
  }

  public class InputTerminalMsg
  {
    public Guid TerminalId { get; init; }
    public string Command { get; init; } = null!;
    public string? Parameter { get; init; }
  }

  public class InputErrorMsg
  {
    public Guid TerminalId { get; init; }
    public string ErrorMessage { get; init; } = null!;
  }

  public class ExecuteCommandMsg
  {
    public Guid CommandId { get; } = Guid.NewGuid();
    public InputTerminalMsg Command { get; init; } = null!;
    public IActorRef OutputTarget { get; init; } = null!;
  }

  public class TerminalOutputMsg
  {
    public Guid TerminalId { get; init; }
    public string Output { get; init; } = null!;
  }

  public class CloseTerminalMsg
  {
    public Guid TerminalId { get; init; }
  }

  // ----- TerminalCommandExe
  public class CommandOutputMsg
  {
    public ExecuteCommandMsg Command { get; init; } = null!;
    public string Output { get; init; } = null!;
  }
}
