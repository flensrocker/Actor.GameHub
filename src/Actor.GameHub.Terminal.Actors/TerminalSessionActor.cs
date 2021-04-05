using System;
using System.Collections.Generic;
using Actor.GameHub.Identity.Abstractions;
using Actor.GameHub.Terminal.Abstractions;
using Actor.GameHub.Terminal.Abtractions;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Event;

namespace Actor.GameHub.Terminal
{
  public class TerminalSessionActor : ReceiveActor
  {
    private readonly ILoggingAdapter _logger = Context.GetLogger();

    private Guid _terminalId;
    private IActorRef? _terminalOrigin;
    private UserLoginSuccessMsg? _userLogin;
    private readonly Dictionary<IActorRef, ExecuteCommandMsg> _commands = new();

    public TerminalSessionActor()
    {
      Become(ReceiveLogin);

      _logger.Info("==> Terminal-Session started");
    }

    private void ReceiveLogin()
    {
      Receive<LoginTerminalMsg>(Login);
      Receive<UserLoginErrorMsg>(LoginError);
      Receive<UserLoginSuccessMsg>(LoginSuccess);
      Receive<Terminated>(OnTerminated);
    }

    private void ReceiveInput()
    {
      Receive<InputTerminalMsg>(msg => msg.TerminalId == _terminalId, Input);
      Receive<CommandOutputMsg>(CommandOutput);
      Receive<CloseTerminalMsg>(msg => msg.TerminalId == _terminalId, Close);
      Receive<Terminated>(OnTerminated);
    }

    private void Login(LoginTerminalMsg loginMsg)
    {
      System.Diagnostics.Debug.Assert(_terminalOrigin is null && _userLogin is null);

      _terminalId = loginMsg.TerminalId;
      _terminalOrigin = Sender;

      _logger.Info($"[Terminal {_terminalId}] login for {loginMsg.LoginUser.Username} from {_terminalOrigin.Path}");

      var mediator = DistributedPubSub.Get(Context.System).Mediator;
      var sendLoginUser = new Send(IdentityMetadata.IdentityPath, loginMsg.LoginUser);
      mediator.Tell(sendLoginUser, Self);

      _logger.Info($"==> Login from {Self.Path}");
    }

    private void LoginError(UserLoginErrorMsg loginErrorMsg)
    {
      System.Diagnostics.Debug.Assert(_terminalOrigin is not null && _userLogin is null);

      _logger.Error($"[Terminal {_terminalId}] login error {loginErrorMsg.ErrorMessage}");

      var terminalErrorMsg = new TerminalOpenErrorMsg
      {
        ErrorMessage = $"terminal error: {loginErrorMsg.ErrorMessage}",
      };
      _terminalOrigin.Tell(terminalErrorMsg, Self);

      Context.System.Stop(Self);
    }

    private void LoginSuccess(UserLoginSuccessMsg loginSuccessMsg)
    {
      System.Diagnostics.Debug.Assert(_terminalOrigin is not null && _userLogin is null);

      _logger.Info($"[Terminal {_terminalId}] user login, send to {_terminalOrigin.Path}");

      _userLogin = loginSuccessMsg;

      Context.Watch(_userLogin.ShellRef);

      var terminalSuccessMsg = new TerminalOpenSuccessMsg
      {
        TerminalId = _terminalId,
        TerminalRef = Self,
      };
      _terminalOrigin.Tell(terminalSuccessMsg, Self);

      Become(ReceiveInput);
    }

    private void Input(InputTerminalMsg inputMsg)
    {
      System.Diagnostics.Debug.Assert(_userLogin is not null);

      var commandMsg = new ExecuteCommandMsg
      {
        Command = inputMsg,
        OutputTarget = Sender,
      };
      var commandExe = Context.ActorOf(TerminalCommandExeActor.Props(), TerminalMetadata.TerminalCommandExeName(commandMsg.CommandId));
      _commands.Add(commandExe, commandMsg);
      commandExe.Tell(commandMsg);
      Context.Watch(commandExe);
    }

    private void CommandOutput(CommandOutputMsg cmdOutputMsg)
    {
      if (_commands.Remove(Sender))
      {
        var outputMsg = new TerminalOutputMsg
        {
          TerminalId = cmdOutputMsg.Command.Command.TerminalId,
          Output = cmdOutputMsg.Output,
        };
        cmdOutputMsg.Command.OutputTarget.Tell(outputMsg);
      }
    }

    private void Close(CloseTerminalMsg closeMsg)
    {
      System.Diagnostics.Debug.Assert(_userLogin is not null);

      var logoutMsg = new LogoutUserMsg
      {
      };
      _userLogin.ShellRef.Tell(logoutMsg);

      Context.System.Stop(Self);
    }

    private void OnTerminated(Terminated terminatedMsg)
    {
      if (_commands.Remove(terminatedMsg.ActorRef, out var command))
      {
        var errorMsg = new InputErrorMsg
        {
          TerminalId = command.Command.TerminalId,
          ErrorMessage = $"unexpected error on command {command.Command.Command} {command.Command.Parameter}",
        };
        command.OutputTarget.Tell(errorMsg);
      }
      else if (terminatedMsg.ActorRef == _userLogin?.ShellRef)
      {
        _logger.Error($"UserLogin terminated, exiting");
        Context.System.Stop(Self);
      }
    }

    public static Props Props()
      => Akka.Actor.Props
        .Create(() => new TerminalSessionActor())
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
