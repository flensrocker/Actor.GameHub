using Actor.GameHub.Terminal.Abstractions;
using Akka.Actor;

namespace Actor.GameHub.Terminal.Actors
{
  public class TerminalEchoCommand : ITerminalCommand
  {
    public string Command { get; } = "echo";
    public Props Props()
      => Akka.Actor.Props
        .Create<TerminalEchoCommandActor>()
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }

  public class TerminalEchoCommandActor : ReceiveActor
  {
    public TerminalEchoCommandActor()
    {
      Receive<ExecuteTerminalCommandMsg>(Execute);
    }

    private void Execute(ExecuteTerminalCommandMsg commandMsg)
    {
      Sender.Tell(new TerminalCommandSuccessMsg
      {
        CommandId = commandMsg.CommandId,
        ExitCode = 0,
        Output = $"{commandMsg.Input.Parameter}",
      });
    }
  }
}
