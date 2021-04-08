using System;
using Actor.GameHub.Terminal.Abstractions;
using Akka.Actor;
using Akka.Event;

namespace Actor.GameHub.Terminal
{
  public class TerminalActor : ReceiveActor
  {
    private readonly ILoggingAdapter _logger = Context.GetLogger();

    public TerminalActor()
    {
      Receive<OpenTerminalMsg>(Open);
      Receive<Terminated>(OnTerminated);

      _logger.Info("==> Terminal started");
    }

    private void Open(OpenTerminalMsg openMsg)
    {
      _logger.Info($"received OpenTerminal from {Sender.Path}");

      var loginMsg = new LoginTerminalMsg
      {
        TerminalId = Guid.NewGuid(),
        LoginUser = openMsg.LoginUser,
      };

      var terminalSession = Context.ActorOf(TerminalSessionActor.Props(), TerminalMetadata.TerminalSessionName(loginMsg.TerminalId));
      Context.Watch(terminalSession);

      terminalSession.Forward(loginMsg);
    }

    private void OnTerminated(Terminated terminatedMsg)
    {
      _logger.Warning($"{nameof(OnTerminated)}: {terminatedMsg}");
    }

    public static Props Props()
      => Akka.Actor.Props.Create<TerminalActor>();
  }
}
