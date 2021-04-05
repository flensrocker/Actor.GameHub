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
    private readonly Dictionary<Guid, (InputTerminalMsg Input, IActorRef InputOrigin)> _inputOriginByInputCommandId = new();

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
      Receive<InputErrorMsg>(InputError);
      Receive<InputSuccessMsg>(InputSuccess);
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
        ErrorMessage = $"terminal error: {loginErrorMsg.ErrorMessage}",
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
      };
      _terminalOrigin.Tell(terminalSuccessMsg, Self);

      Become(ReceiveInput);
    }

    private void Input(InputTerminalMsg inputMsg)
    {
      System.Diagnostics.Debug.Assert(_userLogin is not null);

      var inputCommandMsg = new InputCommandMsg
      {
        InputCommandId = Guid.NewGuid(),
        Command = inputMsg.Command,
        Parameter = inputMsg.Parameter,
      };

      if (_inputOriginByInputCommandId.TryAdd(inputCommandMsg.InputCommandId, (inputMsg, Sender)))
      {
        _userLogin.ShellRef.Tell(inputCommandMsg);
      }
      else
      {
        var terminalErrorMsg = new TerminalErrorMsg
        {
          TerminalId = _terminalId,
          InputId = inputMsg.InputId,
          ErrorMessage = "inputId error, try again...",
        };
        Sender.Tell(terminalErrorMsg);
      }
    }

    private void InputError(InputErrorMsg inputErrorMsg)
    {
      if (_inputOriginByInputCommandId.Remove(inputErrorMsg.InputCommandId, out var data))
      {
        var terminalErrorMsg = new TerminalErrorMsg
        {
          TerminalId = _terminalId,
          InputId = data.Input.InputId,
          ErrorMessage = $"input error: {inputErrorMsg.ErrorMessage}",
        };
        data.InputOrigin.Tell(terminalErrorMsg);
      }
    }

    private void InputSuccess(InputSuccessMsg inputSuccessMsg)
    {
      if (_inputOriginByInputCommandId.Remove(inputSuccessMsg.InputCommandId, out var data))
      {
        var terminalSuccessMsg = new TerminalSuccessMsg
        {
          TerminalId = _terminalId,
          InputId = data.Input.InputId,
          Output = inputSuccessMsg.Output,
        };
        data.InputOrigin.Tell(terminalSuccessMsg);
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
      if (_userLogin is not null && terminatedMsg.ActorRef == _userLogin.ShellRef)
      {
        _logger.Error($"Shell terminated, exiting");
        Context.System.Stop(Self);
      }
    }

    public static Props Props()
      => Akka.Actor.Props
        .Create(() => new TerminalSessionActor())
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
