﻿using System;
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
    private IActorRef? _terminalOutput;
    private UserLoginSuccessMsg? _userLogin;

    public TerminalSessionActor()
    {
      // TODO use Become for login/logged in state
      Receive<LoginTerminalMsg>(Login);
      Receive<UserLoginErrorMsg>(LoginError);
      Receive<UserLoginSuccessMsg>(LoginSuccess);
      Receive<InputTerminalMsg>(msg => msg.TerminalId == _terminalId, Input);
      Receive<CloseTerminalMsg>(msg => msg.TerminalId == _terminalId, Close);
      Receive<Terminated>(OnTerminated);

      _logger.Info("==> Terminal-Session started");
    }

    private void Login(LoginTerminalMsg loginMsg)
    {
      if (_terminalOutput is not null || _userLogin is not null)
        return;

      _terminalId = loginMsg.TerminalId;
      _terminalOutput = Sender;

      _logger.Info($"[Terminal {_terminalId}] login for {loginMsg.LoginUser.Username} from {_terminalOutput.Path}");

      var mediator = DistributedPubSub.Get(Context.System).Mediator;
      var sendLoginUser = new Send(IdentityMetadata.IdentityPath, loginMsg.LoginUser);
      mediator.Tell(sendLoginUser, Self);
    }

    private void LoginError(UserLoginErrorMsg loginErrorMsg)
    {
      if (_terminalOutput is null || _userLogin is not null)
        return;

      _logger.Error($"[Terminal {_terminalId}] login error {loginErrorMsg.ErrorMessage}");

      var terminalErrorMsg = new TerminalOpenErrorMsg
      {
        ErrorMessage = $"terminal error: {loginErrorMsg.ErrorMessage}",
      };
      _terminalOutput.Tell(terminalErrorMsg, Self);

      Context.System.Stop(Self);
    }

    private void LoginSuccess(UserLoginSuccessMsg loginSuccessMsg)
    {
      if (_terminalOutput is null || _userLogin is not null)
        return;

      _logger.Info($"[Terminal {_terminalId}] user login, send to {_terminalOutput!.Path}");

      _userLogin = loginSuccessMsg;

      Context.Watch(_userLogin.UserLogin);

      var terminalSuccessMsg = new TerminalOpenSuccessMsg
      {
        TerminalId = _terminalId,
        TerminalRef = Self,
      };
      _terminalOutput.Tell(terminalSuccessMsg, Self);
    }

    private void Input(InputTerminalMsg inputMsg)
    {
      if (_userLogin is null)
        return;

      var outputMsg = new TerminalOutputMsg
      {
        TerminalId = _terminalId,
        Output = $"{inputMsg.Command} {inputMsg.Parameter}",
      };
      Sender.Tell(outputMsg);
    }

    private void Close(CloseTerminalMsg closeMsg)
    {
      if (_userLogin is not null)
      {
        var logoutMsg = new LogoutUserMsg
        {
        };
        _userLogin.UserLogin.Tell(logoutMsg);
      }

      Context.System.Stop(Self);
    }

    private void OnTerminated(Terminated terminatedMsg)
    {
      if (_userLogin is not null && terminatedMsg.ActorRef == _userLogin.UserLogin)
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
