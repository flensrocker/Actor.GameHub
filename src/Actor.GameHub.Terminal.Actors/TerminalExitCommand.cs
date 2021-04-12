using Actor.GameHub.Terminal.Abstractions;
using Akka.Actor;

namespace Actor.GameHub.Terminal.Actors
{
  public class TerminalExitCommand : ITerminalCommand
  {
    public string Command { get; } = "exit";
    public Props Props()
      => Akka.Actor.Props
        .Create<TerminalExitCommandActor>()
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }

  public class TerminalExitCommandActor : ReceiveActor
  {
    public TerminalExitCommandActor()
    {
      Receive<ExecuteTerminalCommandMsg>(Execute);
    }

    private void Execute(ExecuteTerminalCommandMsg commandMsg)
    {
      Sender.Tell(new TerminalCommandSuccessMsg
      {
        CommandId = commandMsg.CommandId,
        ExitCode = 0,
        Output = "exiting...",
      });
      Sender.Tell(new CloseTerminalMsg
      {
        TerminalId = commandMsg.Input.TerminalId,
      });
    }
  }
}
