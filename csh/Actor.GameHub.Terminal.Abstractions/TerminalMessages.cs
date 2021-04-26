using System;
using Actor.GameHub.Identity.Abstractions;
using Akka.Actor;

namespace Actor.GameHub.Terminal.Abstractions
{
  public interface ITerminalMsg
  {
    Guid TerminalId { get; }
  }

  // ----- Terminal

  public class OpenTerminalMsg
  {
    public string Username { get; init; } = null!;
  }

  // ----- TerminalSession

  public class LoginTerminalMsg : ITerminalMsg
  {
    public Guid TerminalId { get; init; }
    public LoginUserMsg LoginUser { get; init; } = null!;
  }

  public class TerminalOpenErrorMsg
  {
    public string ErrorMessage { get; init; } = null!;
  }

  public class TerminalOpenSuccessMsg : ITerminalMsg
  {
    public Guid TerminalId { get; init; }
    public IActorRef TerminalRef { get; init; } = null!;
    public Guid UserId { get; init; }
    public string Username { get; init; } = null!;
    //public string? AuthToken { get; init; }
  }

  public class InputTerminalMsg : ITerminalMsg
  {
    public Guid TerminalId { get; init; }
    public Guid TerminalInputId { get; init; }
    public string Command { get; init; } = null!;
    public string? Parameter { get; init; }
  }

  public class TerminalInputRejectedMsg : ITerminalMsg
  {
    public enum RejectReasonEnum
    {
      Unknown = 0,
      TerminalClosed = 1,
    }

    public Guid TerminalId { get; init; }
    public RejectReasonEnum Reason { get; init; }
  }

  public class TerminalInputErrorMsg : ITerminalMsg
  {
    public Guid TerminalId { get; init; }
    public Guid TerminalInputId { get; init; }
    public int ExitCode { get; init; }
    public string ErrorMessage { get; init; } = null!;
  }

  public class TerminalInputSuccessMsg : ITerminalMsg
  {
    public Guid TerminalId { get; init; }
    public Guid TerminalInputId { get; init; }
    public int ExitCode { get; init; }
    public string Output { get; init; } = null!;
  }

  public class ExecuteTerminalCommandMsg
  {
    public Guid CommandId { get; init; }
    public InputTerminalMsg Input { get; init; } = null!;
  }

  public class TerminalCommandErrorMsg
  {
    public Guid CommandId { get; init; }
    public int ExitCode { get; init; }
    public string ErrorMessage { get; init; } = null!;
  }

  public class TerminalCommandSuccessMsg
  {
    public Guid CommandId { get; init; }
    public int ExitCode { get; init; }
    public string Output { get; init; } = null!;
  }

  public class CloseTerminalMsg : ITerminalMsg
  {
    public Guid TerminalId { get; init; }
    public Guid? CommandId { get; init; }
  }

  public class TerminalClosedMsg : ITerminalMsg
  {
    public Guid TerminalId { get; init; }
    public Guid? CommandId { get; init; }
    public int ExitCode { get; set; }
  }
}
