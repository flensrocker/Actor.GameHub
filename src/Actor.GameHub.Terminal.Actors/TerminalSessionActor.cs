using System;
using Actor.GameHub.Identity.Abstractions;
using Actor.GameHub.Terminal.Abstractions;
using Akka.Actor;
using Akka.Event;

namespace Actor.GameHub.Terminal
{
  public class TerminalSessionActor : ReceiveActor
  {
    private readonly ILoggingAdapter _logger = Context.GetLogger();

    private Guid _terminalId;
    private IActorRef? _terminalOutput;
    private UserLoginSuccessMsg? _loginSuccess;

    public TerminalSessionActor()
    {
      // TODO use Become for login/logged in state
      Receive<LoginTerminalMsg>(Login);
      Receive<UserLoginErrorMsg>(LoginError);
      Receive<UserLoginSuccessMsg>(LoginSuccess);
      Receive<InputTerminalMsg>(msg => msg.TerminalId == _terminalId, Input);
      Receive<CloseTerminalMsg>(msg => msg.TerminalId == _terminalId, Close);

      _logger.Info("==> Terminal-Session started");
    }

    private void Login(LoginTerminalMsg loginMsg)
    {
      if (_terminalOutput is not null || _loginSuccess is not null)
        return;

      _terminalId = loginMsg.TerminalId;
      _terminalOutput = Sender;

      _logger.Info($"[Terminal {_terminalId}] login for {loginMsg.LoginUser.Username} from {_terminalOutput.Path}");

      Context.System
        .ActorSelection(IdentityMetadata.IdentityPath)
        .Tell(loginMsg.LoginUser, Self);
    }

    private void LoginError(UserLoginErrorMsg loginErrorMsg)
    {
      if (_terminalOutput is null || _loginSuccess is not null)
        return;

      _logger.Error($"[Terminal {_terminalId}] login error {loginErrorMsg.ErrorMessage}");

      var terminalErrorMsg = new TerminalOpenErrorMsg
      {
        ErrorMessage = $"terminal error: {loginErrorMsg.ErrorMessage}",
      };
      _terminalOutput.Tell(terminalErrorMsg);

      Context.System.Stop(Self);
    }

    private void LoginSuccess(UserLoginSuccessMsg loginSuccessMsg)
    {
      if (_terminalOutput is null || _loginSuccess is not null)
        return;

      _loginSuccess = loginSuccessMsg;

      _logger.Info($"[Terminal {_terminalId}] login success");

      var terminalSuccessMsg = new TerminalOpenSuccessMsg
      {
        TerminalId = _terminalId,
        TerminalRef = Self,
      };
      _terminalOutput.Tell(terminalSuccessMsg);
    }

    private void Input(InputTerminalMsg inputMsg)
    {
      if (_loginSuccess is null)
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
      Context.System.Stop(Self);
    }

    public static Props Props()
      => Akka.Actor.Props
        .Create(() => new TerminalSessionActor())
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
