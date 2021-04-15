using System;
using Actor.GameHub.Terminal.Abstractions;
using Akka.Actor;

namespace Actor.GameHub.Terminal.Actors
{
  public class TerminalSleepCommand : ITerminalCommand
  {
    public string Command { get; } = "sleep";
    public Props Props()
      => Akka.Actor.Props
        .Create<TerminalSleepCommandActor>()
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }

  public class TerminalSleepCommandActor : ReceiveActor, IWithTimers
  {
    class SleepWakeupMsg
    {
      public ExecuteTerminalCommandMsg Command { get; init; } = null!;
      public IActorRef CommandOrigin { get; init; } = null!;
    }

    public ITimerScheduler Timers { get; set; } = null!;

    private Guid _commandId;

    public TerminalSleepCommandActor()
    {
      Become(ReceiveCommand);
    }

    private void ReceiveCommand()
    {
      Receive<ExecuteTerminalCommandMsg>(Execute);
    }

    private void ReceiveWakeup()
    {
      Receive<SleepWakeupMsg>(msg => msg.Command.CommandId == _commandId, Wakeup);
    }

    private void Execute(ExecuteTerminalCommandMsg commandMsg)
    {
      if (string.IsNullOrWhiteSpace(commandMsg.Input.Parameter)
        || !int.TryParse(commandMsg.Input.Parameter, out var sleepSeconds)
        || sleepSeconds < 1)
      {
        var errorMsg = new TerminalCommandErrorMsg
        {
          CommandId = commandMsg.CommandId,
          ExitCode = 400,
          ErrorMessage = "Sleep timeout must be a positive integer.",
        };
        Sender.Tell(errorMsg);
      }
      else
      {
        _commandId = commandMsg.CommandId;

        var wakeupMsg = new SleepWakeupMsg
        {
          Command = commandMsg,
          CommandOrigin = Sender,
        };
        Timers.StartSingleTimer("wakeup", wakeupMsg, TimeSpan.FromSeconds(sleepSeconds));
        Become(ReceiveWakeup);
      }
    }

    private void Wakeup(SleepWakeupMsg wakeupMsg)
    {
      wakeupMsg.CommandOrigin.Tell(new TerminalCommandSuccessMsg
      {
        CommandId = wakeupMsg.Command.CommandId,
        ExitCode = 0,
        Output = "",
      });
    }
  }
}
