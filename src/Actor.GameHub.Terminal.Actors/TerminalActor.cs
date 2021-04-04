﻿using System;
using Actor.GameHub.Terminal.Abstractions;
using Actor.GameHub.Terminal.Abtractions;
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

    private void Open(OpenTerminalMsg addMsg)
    {
      _logger.Info($"open terminal from sender {Sender.Path}");

      var loginMsg = new LoginTerminalMsg
      {
        LoginUser = addMsg.LoginUser,
      };

      var remoteAddress = Sender.Path.Address;
      var terminalSession = Context.ActorOf(
        TerminalSessionActor.Props().WithDeploy(Deploy.None.WithScope(new RemoteScope(remoteAddress))),
        TerminalMetadata.TerminalSessionName(loginMsg.TerminalId));
      Context.Watch(terminalSession);

      terminalSession.Forward(loginMsg);
    }

    private void OnTerminated(Terminated terminatedMsg)
    {
      _logger.Warning($"{nameof(OnTerminated)}: {terminatedMsg}");
    }

    public static Props Props()
      => Akka.Actor.Props.Create(() => new TerminalActor());
  }
}
