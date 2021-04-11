using Actor.GameHub.Identity.Abstractions;
using Akka.Actor;

namespace Actor.GameHub.Identity.Actors
{
  public class ShellCommandActor : ReceiveActor
  {
    public ShellCommandActor()
    {
      Receive<ExecuteCommandMsg>(Execute);
    }

    private void Execute(ExecuteCommandMsg commandMsg)
    {
      if (commandMsg.Input.Command == "echo")
      {
        var outputMsg = new CommandSuccessMsg
        {
          CommandId = commandMsg.CommandId,
          ExitCode = 0,
          Output = $"{commandMsg.Input.Parameter}",
        };
        Sender.Tell(outputMsg);
      }
      else if (commandMsg.Input.Command == "error")
      {
        var exitCode = 0;

        if (!string.IsNullOrWhiteSpace(commandMsg.Input.Parameter))
          _ = int.TryParse(commandMsg.Input.Parameter, out exitCode);

        var errorMsg = new CommandErrorMsg
        {
          CommandId = commandMsg.CommandId,
          ExitCode = exitCode,
          ErrorMessage = $"{commandMsg.Input.Parameter}",
        };
        Sender.Tell(errorMsg);
      }
    }

    public static Props Props()
      => Akka.Actor.Props
        .Create<ShellCommandActor>()
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
