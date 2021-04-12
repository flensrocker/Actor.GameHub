using System;
using System.Collections.Generic;
using Actor.GameHub.Identity.Abstractions;
using Actor.GameHub.Terminal.Abstractions;
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
    private readonly Dictionary<Guid, (InputTerminalMsg Input, IActorRef InputOrigin)> _inputOriginByShellInputId = new();

    public TerminalSessionActor()
    {
      Become(ReceiveLogin);
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
      Receive<ShellInputErrorMsg>(InputError);
      Receive<ShellInputSuccessMsg>(InputSuccess);
      Receive<ShellExitMsg>(ShellExit);
      Receive<CloseTerminalMsg>(msg => msg.TerminalId == _terminalId, Close);
      Receive<Terminated>(OnTerminated);
    }

    private void Login(LoginTerminalMsg loginMsg)
    {
      System.Diagnostics.Debug.Assert(_terminalOrigin is null && _userLogin is null);

      _terminalId = loginMsg.TerminalId;
      _terminalOrigin = Sender;

      var mediator = DistributedPubSub.Get(Context.System).Mediator;
      var sendLoginUser = new Send(IdentityMetadata.IdentityPath, loginMsg.LoginUser);
      mediator.Tell(sendLoginUser, Self);
    }

    private void LoginError(UserLoginErrorMsg loginErrorMsg)
    {
      System.Diagnostics.Debug.Assert(_terminalOrigin is not null && _userLogin is null);

      var terminalErrorMsg = new TerminalOpenErrorMsg
      {
        ErrorMessage = loginErrorMsg.ErrorMessage,
      };
      _terminalOrigin.Tell(terminalErrorMsg, Self);

      Context.System.Stop(Self);
    }

    private void LoginSuccess(UserLoginSuccessMsg loginSuccessMsg)
    {
      System.Diagnostics.Debug.Assert(_terminalOrigin is not null && _userLogin is null);

      _userLogin = loginSuccessMsg;

      Context.Watch(_userLogin.ShellRef);

      var terminalSuccessMsg = new TerminalOpenSuccessMsg
      {
        TerminalId = _terminalId,
        TerminalRef = Self,
        UserId = loginSuccessMsg.User.UserId,
        Username = loginSuccessMsg.User.Username,
      };
      _terminalOrigin.Tell(terminalSuccessMsg, Self);

      Become(ReceiveInput);
    }

    private void Input(InputTerminalMsg inputTerminalMsg)
    {
      System.Diagnostics.Debug.Assert(_userLogin is not null);

      var inputShellMsg = new InputShellMsg
      {
        UserLoginId = _userLogin.UserLoginId,
        ShellInputId = Guid.NewGuid(),
        Command = inputTerminalMsg.Command,
        Parameter = inputTerminalMsg.Parameter,
      };

      if (_inputOriginByShellInputId.TryAdd(inputShellMsg.ShellInputId, (inputTerminalMsg, Sender)))
      {
        _userLogin.ShellRef.Tell(inputShellMsg);
      }
      else
      {
        var terminalErrorMsg = new TerminalInputErrorMsg
        {
          TerminalId = _terminalId,
          TerminalInputId = inputTerminalMsg.TerminalInputId,
          ErrorMessage = "inputId error, try again...",
        };
        Sender.Tell(terminalErrorMsg);
      }
    }

    private void InputError(ShellInputErrorMsg inputErrorMsg)
    {
      if (_inputOriginByShellInputId.Remove(inputErrorMsg.ShellInputId, out var data))
      {
        var terminalErrorMsg = new TerminalInputErrorMsg
        {
          TerminalId = _terminalId,
          TerminalInputId = data.Input.TerminalInputId,
          ErrorMessage = inputErrorMsg.ErrorMessage,
        };
        data.InputOrigin.Tell(terminalErrorMsg);
      }
    }

    private void InputSuccess(ShellInputSuccessMsg inputSuccessMsg)
    {
      if (_inputOriginByShellInputId.Remove(inputSuccessMsg.ShellInputId, out var data))
      {
        var terminalSuccessMsg = new TerminalInputSuccessMsg
        {
          TerminalId = _terminalId,
          TerminalInputId = data.Input.TerminalInputId,
          Output = inputSuccessMsg.Output,
        };
        data.InputOrigin.Tell(terminalSuccessMsg);
      }
    }

    private void ShellExit(ShellExitMsg exitMsg)
    {
      System.Diagnostics.Debug.Assert(_userLogin is not null);

      if (_inputOriginByShellInputId.Remove(exitMsg.ShellInputId, out var data))
      {
        var terminalClosedMsg = new TerminalClosedMsg
        {
          TerminalId = _terminalId,
          TerminalInputId = data.Input.TerminalInputId,
          ExitCode = exitMsg.ExitCode,
        };
        data.InputOrigin.Tell(terminalClosedMsg);

        Context.Unwatch(_userLogin.ShellRef);
        Context.System.Stop(Self);
      }
    }

    private void Close(CloseTerminalMsg closeMsg)
    {
      System.Diagnostics.Debug.Assert(_userLogin is not null);

      var exitMsg = new InputShellMsg
      {
        UserLoginId = _userLogin.UserLoginId,
        ShellInputId = Guid.NewGuid(),
        Command = "exit",
        Parameter = "0",
      };
      _userLogin.ShellRef.Tell(exitMsg);

      Context.Unwatch(_userLogin.ShellRef);
      Context.System.Stop(Self);
    }

    private void OnTerminated(Terminated terminatedMsg)
    {
      if (_userLogin is not null)
      {
        _logger.Warning($"Shell {_userLogin.UserLoginId} terminated, exiting");

        foreach (var kv in _inputOriginByShellInputId)
        {
          var closedMsg = new TerminalClosedMsg
          {
            TerminalId = _terminalId,
            TerminalInputId = kv.Key,
            ExitCode = -1,
          };
          kv.Value.InputOrigin.Tell(closedMsg, ActorRefs.NoSender);
        }

        Context.System.Stop(Self);
      }
    }

    public static Props Props()
      => Akka.Actor.Props
        .Create<TerminalSessionActor>()
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
