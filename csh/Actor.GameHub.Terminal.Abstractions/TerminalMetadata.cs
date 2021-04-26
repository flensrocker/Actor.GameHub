using System;

namespace Actor.GameHub.Terminal.Abstractions
{
  public static class TerminalMetadata
  {
    public static readonly string TerminalName = "Terminal";
    public static readonly string TerminalPath = $"/user/{TerminalName}";

    public static string TerminalSessionName(Guid terminalId) => $"TerminalSession-{terminalId}";
    public static string TerminalCommandName(Guid commandId) => $"TerminalCommand-{commandId}";
  }
}
