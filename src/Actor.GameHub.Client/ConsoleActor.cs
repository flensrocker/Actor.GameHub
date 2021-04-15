using System;
using Actor.GameHub.Terminal;
using Actor.GameHub.Terminal.Abstractions;
using Akka.Actor;
using Akka.Cluster.Tools.Client;

namespace Actor.GameHub.Client
{
  public class ConsoleActor : ReceiveActor
  {
    private string _prompt = "";

    private IActorRef _clusterClient = null!;
    private TerminalOpenSuccessMsg? _terminalSession;

    public ConsoleActor()
    {
      Become(ReceiveLogin);
    }

    private bool MsgIsAllowed(ITerminalMsg msg)
      => _terminalSession is not null && _terminalSession.TerminalId == msg.TerminalId;

    private void ReceiveLogin()
    {
      _prompt = "login: ";

      Receive<InputConsoleMsg>(Login);
      Receive<TerminalOpenErrorMsg>(OpenError);
      Receive<TerminalOpenSuccessMsg>(OpenSuccess);
    }

    private void ReceiveCommand()
    {
      System.Diagnostics.Debug.Assert(_terminalSession is not null);

      _prompt = $"[{_terminalSession.Username}]> ";

      Receive<InputConsoleMsg>(Command);
      Receive<TerminalInputErrorMsg>(MsgIsAllowed, InputError);
      Receive<TerminalInputSuccessMsg>(MsgIsAllowed, InputSuccess);
      Receive<TerminalClosedMsg>(MsgIsAllowed, OnClose);
      Receive<Terminated>(OnTerminated);
    }

    protected override void PreStart()
    {
      _clusterClient = Context.System.ActorOf(ClusterClient.Props(ClusterClientSettings.Create(Context.System)));

      Console.Write(_prompt);
    }

    protected override void PostStop()
    {
      if (_terminalSession is not null)
      {
        var closeMsg = new CloseTerminalMsg
        {
          TerminalId = _terminalSession.TerminalId,
          CommandId = Guid.NewGuid(),
        };
        _terminalSession.TerminalRef.Tell(closeMsg);

        Context.Unwatch(_terminalSession.TerminalRef);
        _terminalSession = null;
      }
    }

    private void Login(InputConsoleMsg inputMsg)
    {
      if (string.IsNullOrWhiteSpace(inputMsg.Input))
      {
        Console.Write(_prompt);
        return;
      }

      var openMsg = new OpenTerminalMsg
      {
        Username = inputMsg.Input,
      };
      _clusterClient.Tell(new ClusterClient.Send(TerminalMetadata.TerminalPath, openMsg));
    }

    private void OpenError(TerminalOpenErrorMsg terminalErrorMsg)
    {
      Console.Error.WriteLine($"[ERROR] {terminalErrorMsg.ErrorMessage}");
      Console.Write(_prompt);
    }

    private void OpenSuccess(TerminalOpenSuccessMsg terminalSession)
    {
      Console.WriteLine($"terminal opened for user {terminalSession.UserId} with terminalId {terminalSession.TerminalId}");

      _terminalSession = terminalSession;
      Context.Watch(_terminalSession.TerminalRef);

      Become(ReceiveCommand);
      Console.Write(_prompt);
    }

    private void Command(InputConsoleMsg inputMsg)
    {
      System.Diagnostics.Debug.Assert(_terminalSession is not null);

      var command = inputMsg.Input.SplitFirstWord(out var parameter);
      if (string.IsNullOrWhiteSpace(command))
      {
        Console.Write(_prompt);
        return;
      }

      var inputTerminalMsg = new InputTerminalMsg
      {
        TerminalId = _terminalSession.TerminalId,
        TerminalInputId = Guid.NewGuid(),
        Command = command,
        Parameter = parameter,
      };
      _terminalSession.TerminalRef.Tell(inputTerminalMsg);
    }

    private void InputError(TerminalInputErrorMsg inputErrorMsg)
    {
      Console.Error.WriteLine($"[ERROR {inputErrorMsg.ExitCode}] {inputErrorMsg.ErrorMessage}");
      Console.Write(_prompt);
    }

    private void InputSuccess(TerminalInputSuccessMsg inputSuccessMsg)
    {
      Console.WriteLine(inputSuccessMsg.Output);
      Console.Write(_prompt);
    }

    private void OnClose(TerminalClosedMsg closedMsg)
    {
      System.Diagnostics.Debug.Assert(_terminalSession is not null);

      Console.WriteLine($"closed with exit-code {closedMsg.ExitCode}");

      Context.Unwatch(_terminalSession.TerminalRef);
      _terminalSession = null;

      Become(ReceiveLogin);
      Console.Write(_prompt);
    }

    private void OnTerminated(Terminated terminatedMsg)
    {
      if (_terminalSession is not null)
      {
        Console.Error.WriteLine($"[ERROR] Terminal {_terminalSession.TerminalId} terminated");

        var closedMsg = new TerminalClosedMsg
        {
          TerminalId = _terminalSession.TerminalId,
          CommandId = null,
          ExitCode = -1,
        };
        OnClose(closedMsg);
      }
    }

    public static Props Props()
      => Akka.Actor.Props.Create<ConsoleActor>();
  }
}
