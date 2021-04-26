using Akka.Actor;

namespace Actor.GameHub.Terminal.Abstractions
{
  public interface ITerminalCommand
  {
    string Command { get; }
    Props Props();
  }
}
