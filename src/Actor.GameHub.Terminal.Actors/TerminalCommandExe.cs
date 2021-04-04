using Actor.GameHub.Terminal.Abstractions;
using Akka.Actor;

namespace Actor.GameHub.Terminal
{
  public class TerminalCommandExeActor : ReceiveActor
  {
    public TerminalCommandExeActor()
    {
      Receive<ExecuteCommandMsg>(Execute);
    }

    private void Execute(ExecuteCommandMsg commandMsg)
    {
      if (commandMsg.Command.Command == "error")
      {
        Context.System.Stop(Self);
        return;
      }

      var outputMsg = new CommandOutputMsg
      {
        Command = commandMsg,
        Output = $"{commandMsg.Command.Command} {commandMsg.Command.Parameter}",
      };
      Sender.Tell(outputMsg);
    }

    public static Props Props()
      => Akka.Actor.Props
        .Create(() => new TerminalCommandExeActor())
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
