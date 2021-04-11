using System;
using System.Collections.Generic;
using Actor.GameHub.Terminal.Abstractions;
using Akka.Actor;
using Akka.Cluster.Tools.Client;
using Akka.Event;

namespace Actor.GameHub.Client
{
  public class ConsoleActor : ReceiveActor
  {
    private readonly ILoggingAdapter _logger = Context.GetLogger();

    private IActorRef? _openSenderRef;
    private readonly Dictionary<Guid, IActorRef> _inputSender = new();
    private IActorRef _clusterClient = null!;
    private TerminalOpenSuccessMsg? _terminalSession;

    public ConsoleActor()
    {
      Become(ReceiveOpen);

      _logger.Info("==> Console started");
    }

    private void ReceiveOpen()
    {
      Receive<OpenTerminalMsg>(Open);
      Receive<TerminalOpenErrorMsg>(OpenError);
      Receive<TerminalOpenSuccessMsg>(OpenSuccess);
    }

    private void ReceiveInput()
    {
      Receive<InputTerminalMsg>(Input);
      Receive<TerminalInputErrorMsg>(InputError);
      Receive<TerminalInputSuccessMsg>(InputSuccess);
      Receive<CloseTerminalMsg>(Close);
      Receive<TerminalClosedMsg>(OnClose);
      Receive<Terminated>(OnTerminated);
    }

    protected override void PreStart()
    {
      _clusterClient = Context.System.ActorOf(ClusterClient.Props(ClusterClientSettings.Create(Context.System)));
    }

    private void Open(OpenTerminalMsg openMsg)
    {
      _openSenderRef = Sender;
      _clusterClient.Tell(new ClusterClient.Send(TerminalMetadata.TerminalPath, openMsg));
    }

    private void OpenError(TerminalOpenErrorMsg terminalErrorMsg)
    {
      System.Diagnostics.Debug.Assert(_openSenderRef is not null);

      _openSenderRef.Forward(terminalErrorMsg);
      _openSenderRef = null;
    }

    private void OpenSuccess(TerminalOpenSuccessMsg terminalSession)
    {
      System.Diagnostics.Debug.Assert(_terminalSession is null && _openSenderRef is not null);

      _terminalSession = terminalSession;
      Context.Watch(_terminalSession.TerminalRef);
      _openSenderRef.Forward(terminalSession);
      _openSenderRef = null;

      Become(ReceiveInput);
    }

    private void Input(InputTerminalMsg inputTerminalMsg)
    {
      System.Diagnostics.Debug.Assert(_terminalSession is not null);

      _inputSender.Add(inputTerminalMsg.TerminalInputId, Sender);
      _terminalSession.TerminalRef.Tell(inputTerminalMsg);
    }

    private void InputError(TerminalInputErrorMsg inputErrorMsg)
    {
      if (_inputSender.Remove(inputErrorMsg.TerminalInputId, out var inputSender))
        inputSender.Forward(inputErrorMsg);
    }

    private void InputSuccess(TerminalInputSuccessMsg inputSuccessMsg)
    {
      if (_inputSender.Remove(inputSuccessMsg.TerminalInputId, out var inputSender))
        inputSender.Forward(inputSuccessMsg);
    }

    private void Close(CloseTerminalMsg closeMsg)
    {
      System.Diagnostics.Debug.Assert(_terminalSession is not null);

      _terminalSession.TerminalRef.Tell(closeMsg);
      Context.Unwatch(_terminalSession.TerminalRef);

      _terminalSession = null;
      _openSenderRef = null;
      _inputSender.Clear();

      Become(ReceiveOpen);
    }

    private void OnClose(TerminalClosedMsg closedMsg)
    {
      System.Diagnostics.Debug.Assert(_terminalSession is not null);

      if (_inputSender.Remove(closedMsg.TerminalInputId, out var inputSender))
      {
        inputSender.Forward(closedMsg);

        Context.Unwatch(_terminalSession.TerminalRef);
        _terminalSession = null;
        _openSenderRef = null;
        _inputSender.Clear();

        Become(ReceiveOpen);
      }
    }

    private void OnTerminated(Terminated terminatedMsg)
    {
      if (_terminalSession is not null)
      {
        _logger.Warning($"Terminal {_terminalSession.TerminalId} terminated");

        foreach (var kv in _inputSender)
        {
          var closedMsg = new TerminalClosedMsg
          {
            TerminalId = _terminalSession.TerminalId,
            TerminalInputId = kv.Key,
            ExitCode = -1,
          };
          kv.Value.Tell(closedMsg);
        }

        _terminalSession = null;
        _openSenderRef = null;
        _inputSender.Clear();

        Become(ReceiveOpen);
      }
    }

    public static Props Props()
      => Akka.Actor.Props.Create<ConsoleActor>();
  }
}
