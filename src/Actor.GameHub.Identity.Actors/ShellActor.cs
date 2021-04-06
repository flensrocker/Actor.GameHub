using System;
using System.Collections.Generic;
using Actor.GameHub.Identity.Abstractions;
using Akka.Actor;
using Akka.Event;

namespace Actor.GameHub.Identity.Actors
{
  public class ShellActor : ReceiveActor
  {
    private readonly ILoggingAdapter _logger = Context.GetLogger();

    private AddUserLoginMsg? _userLogin;
    private readonly Dictionary<Guid, (InputShellMsg Input, IActorRef InputOrigin)> _inputOriginByCommandId = new();
    private readonly Dictionary<IActorRef, Guid> _commandIdByShellCommandRef = new();

    public ShellActor()
    {
      Become(ReceiveLogin);
    }

    private void ReceiveLogin()
    {
      Receive<AddUserLoginMsg>(AddLogin);
      Receive<Terminated>(OnTerminated);
    }

    private void ReceiveLoggedIn()
    {
      Receive<InputShellMsg>(msg => msg.UserLoginId == _userLogin?.UserLoginId, Input);
      Receive<CommandErrorMsg>(CommandError);
      Receive<CommandSuccessMsg>(CommandSuccess);
      Receive<LogoutUserMsg>(msg => msg.UserLoginId == _userLogin?.UserLoginId, LogoutUser);
      Receive<Terminated>(OnTerminated);
    }

    private void AddLogin(AddUserLoginMsg addLoginMsg)
    {
      System.Diagnostics.Debug.Assert(_userLogin is null);

      _userLogin = addLoginMsg;

      var loginSuccessMsg = new UserLoginSuccessMsg
      {
        UserLoginId = _userLogin.UserLoginId,
        ShellRef = Self,
        User = _userLogin.User,
      };
      _userLogin.LoginOrigin.Tell(loginSuccessMsg);
      Context.Watch(_userLogin.LoginOrigin);
      _logger.Info($"==> Watch login-origin {_userLogin.LoginOrigin.Path}");

      Become(ReceiveLoggedIn);
    }

    private void Input(InputShellMsg inputMsg)
    {
      System.Diagnostics.Debug.Assert(_userLogin is not null);

      var executeMsg = new ExecuteCommandMsg
      {
        CommandId = Guid.NewGuid(),
        Input = inputMsg,
      };

      if (_inputOriginByCommandId.TryAdd(executeMsg.CommandId, (inputMsg, Sender)))
      {
        var shellCommand = Context.ActorOf(ShellCommandActor.Props(), IdentityMetadata.ShellCommandName(executeMsg.CommandId));
        _commandIdByShellCommandRef.Add(shellCommand, executeMsg.CommandId);

        Context.Watch(shellCommand);
        shellCommand.Tell(executeMsg);
      }
      else
      {
        var inputErrorMsg = new ShellInputErrorMsg
        {
          UserLoginId = _userLogin.UserLoginId,
          ShellInputId = inputMsg.ShellInputId,
          ErrorMessage = "commandId error, try again...",
        };
        Sender.Tell(inputErrorMsg);
      }
    }

    private void CommandError(CommandErrorMsg commandErrorMsg)
    {
      System.Diagnostics.Debug.Assert(_userLogin is not null);

      if (_inputOriginByCommandId.TryGetValue(commandErrorMsg.CommandId, out var data)
        && _commandIdByShellCommandRef.ContainsKey(Sender))
      {
        var inputErrorMsg = new ShellInputErrorMsg
        {
          UserLoginId = _userLogin.UserLoginId,
          ShellInputId = data.Input.ShellInputId,
          ErrorMessage = commandErrorMsg.ErrorMessage,
        };
        data.InputOrigin.Tell(inputErrorMsg);

        _inputOriginByCommandId.Remove(commandErrorMsg.CommandId);
        _commandIdByShellCommandRef.Remove(Sender);
        Context.Stop(Sender);
      }
    }

    private void CommandSuccess(CommandSuccessMsg commandSuccessMsg)
    {
      System.Diagnostics.Debug.Assert(_userLogin is not null);

      if (_inputOriginByCommandId.TryGetValue(commandSuccessMsg.CommandId, out var data)
        && _commandIdByShellCommandRef.ContainsKey(Sender))
      {
        var inputSuccessMsg = new ShellInputSuccessMsg
        {
          UserLoginId = _userLogin.UserLoginId,
          ShellInputId = data.Input.ShellInputId,
          Output = commandSuccessMsg.Output,
        };
        data.InputOrigin.Tell(inputSuccessMsg);

        _inputOriginByCommandId.Remove(commandSuccessMsg.CommandId);
        _commandIdByShellCommandRef.Remove(Sender);
        Context.Stop(Sender);
      }
    }

    private void LogoutUser(LogoutUserMsg logoutMsg)
    {
      System.Diagnostics.Debug.Assert(_userLogin is not null);

      _logger.Info($"==> Unwatch login-origin {_userLogin.LoginOrigin.Path}");
      var loginOrigin = _userLogin.LoginOrigin;
      _userLogin = null;
      Context.Unwatch(loginOrigin);
      Context.System.Stop(Self);
    }

    private void OnTerminated(Terminated terminatedMsg)
    {
      var commandRef = terminatedMsg.ActorRef;

      if (_commandIdByShellCommandRef.TryGetValue(commandRef, out var commandId)
        && _inputOriginByCommandId.TryGetValue(commandId, out var data))
      {
        _logger.Warning($"{nameof(OnTerminated)}: unexpected stop of command {commandId}, {commandRef.Path}");
        _commandIdByShellCommandRef.Remove(commandRef);
        _inputOriginByCommandId.Remove(commandId);

        var inputErrorMsg = new ShellInputErrorMsg
        {
          ShellInputId = data.Input.ShellInputId,
          ErrorMessage = "shell error: unexpected stop of command",
        };
        data.InputOrigin.Tell(inputErrorMsg);
      }
      else if (_userLogin is not null && terminatedMsg.ActorRef == _userLogin.LoginOrigin)
      {
        _logger.Warning($"==> login-origin {_userLogin.LoginOrigin.Path} terminated, stopping");
        Context.System.Stop(Self);
      }
    }

    public static Props Props()
      => Akka.Actor.Props
        .Create(() => new ShellActor())
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
