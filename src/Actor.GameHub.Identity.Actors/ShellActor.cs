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
    private readonly Dictionary<Guid, (InputCommandMsg Input, IActorRef InputOrigin)> _inputOriginByCommandId = new();
    private readonly Dictionary<IActorRef, Guid> _commandIdByCommandRef = new();

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
      Receive<InputCommandMsg>(Input);
      Receive<CommandErrorMsg>(CommandError);
      Receive<CommandSuccessMsg>(CommandSuccess);
      Receive<LogoutUserMsg>(LogoutUser);
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

    private void Input(InputCommandMsg inputMsg)
    {
      var executeMsg = new ExecuteCommandMsg
      {
        CommandId = Guid.NewGuid(),
        Input = inputMsg,
      };

      if (_inputOriginByCommandId.TryAdd(executeMsg.CommandId, (inputMsg, Sender)))
      {
        var shellCommand = Context.ActorOf(ShellCommandActor.Props(), IdentityMetadata.ShellCommandName(executeMsg.CommandId));
        _commandIdByCommandRef.Add(shellCommand, executeMsg.CommandId);

        Context.Watch(shellCommand);
        shellCommand.Tell(executeMsg);
      }
      else
      {
        var inputErrorMsg = new InputErrorMsg
        {
          InputCommandId = inputMsg.InputCommandId,
          ErrorMessage = "commandId error, try again...",
        };
        Sender.Tell(inputErrorMsg);
      }
    }

    private void CommandError(CommandErrorMsg commandErrorMsg)
    {
      if (_inputOriginByCommandId.TryGetValue(commandErrorMsg.CommandId, out var data)
        && _commandIdByCommandRef.ContainsKey(Sender))
      {
        var inputErrorMsg = new InputErrorMsg
        {
          InputCommandId = data.Input.InputCommandId,
          ErrorMessage = $"command error: {commandErrorMsg.ErrorMessage}",
        };
        data.InputOrigin.Tell(inputErrorMsg);

        _inputOriginByCommandId.Remove(commandErrorMsg.CommandId);
        _commandIdByCommandRef.Remove(Sender);
        Context.Stop(Sender);
      }
    }

    private void CommandSuccess(CommandSuccessMsg commandSuccessMsg)
    {
      if (_inputOriginByCommandId.TryGetValue(commandSuccessMsg.CommandId, out var data)
        && _commandIdByCommandRef.ContainsKey(Sender))
      {
        var inputSuccessMsg = new InputSuccessMsg
        {
          InputCommandId = data.Input.InputCommandId,
          Output = commandSuccessMsg.Output,
        };
        data.InputOrigin.Tell(inputSuccessMsg);

        _inputOriginByCommandId.Remove(commandSuccessMsg.CommandId);
        _commandIdByCommandRef.Remove(Sender);
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

      if (_commandIdByCommandRef.TryGetValue(commandRef, out var commandId)
        && _inputOriginByCommandId.TryGetValue(commandId, out var data))
      {
        _logger.Warning($"{nameof(OnTerminated)}: unexpected stop of command {commandId}, {commandRef.Path}");
        _commandIdByCommandRef.Remove(commandRef);
        _inputOriginByCommandId.Remove(commandId);

        var inputErrorMsg = new InputErrorMsg
        {
          InputCommandId = data.Input.InputCommandId,
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
