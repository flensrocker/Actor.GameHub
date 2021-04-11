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
    private int _lastCommandExitCode = 0;
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
      Receive<InputShellMsg>(msg => msg.UserLoginId == _userLogin?.UserLoginId && msg.Command == "exit", Exit);
      Receive<InputShellMsg>(msg => msg.UserLoginId == _userLogin?.UserLoginId, Input);
      Receive<CommandErrorMsg>(CommandError);
      Receive<CommandSuccessMsg>(CommandSuccess);
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

      Become(ReceiveLoggedIn);
    }

    private void Exit(InputShellMsg inputMsg)
    {
      System.Diagnostics.Debug.Assert(_userLogin is not null);

      Sender.Tell(new ShellExitMsg
      {
        UserLoginId = _userLogin.UserLoginId,
        ShellInputId = inputMsg.ShellInputId,
        ExitCode = _lastCommandExitCode,
      });

      _userLogin = null;
      Context.System.Stop(Self);
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

      var commandRef = Sender;
      if (_inputOriginByCommandId.TryGetValue(commandErrorMsg.CommandId, out var data)
        && _commandIdByShellCommandRef.ContainsKey(commandRef))
      {
        _lastCommandExitCode = commandErrorMsg.ExitCode;

        // TODO Remove
        if (_lastCommandExitCode < 0)
        {
          Context.System.Stop(Self);
          return;
        }

        var inputErrorMsg = new ShellInputErrorMsg
        {
          UserLoginId = _userLogin.UserLoginId,
          ShellInputId = data.Input.ShellInputId,
          ExitCode = commandErrorMsg.ExitCode,
          ErrorMessage = commandErrorMsg.ErrorMessage,
        };
        data.InputOrigin.Tell(inputErrorMsg);

        _inputOriginByCommandId.Remove(commandErrorMsg.CommandId);
        _commandIdByShellCommandRef.Remove(commandRef);

        Context.Unwatch(commandRef);
        Context.Stop(commandRef);
      }
    }

    private void CommandSuccess(CommandSuccessMsg commandSuccessMsg)
    {
      System.Diagnostics.Debug.Assert(_userLogin is not null);

      var commandRef = Sender;
      if (_inputOriginByCommandId.TryGetValue(commandSuccessMsg.CommandId, out var data)
        && _commandIdByShellCommandRef.ContainsKey(commandRef))
      {
        _lastCommandExitCode = commandSuccessMsg.ExitCode;

        var inputSuccessMsg = new ShellInputSuccessMsg
        {
          UserLoginId = _userLogin.UserLoginId,
          ShellInputId = data.Input.ShellInputId,
          ExitCode = commandSuccessMsg.ExitCode,
          Output = commandSuccessMsg.Output,
        };
        data.InputOrigin.Tell(inputSuccessMsg);

        _inputOriginByCommandId.Remove(commandSuccessMsg.CommandId);
        _commandIdByShellCommandRef.Remove(commandRef);

        Context.Unwatch(commandRef);
        Context.Stop(commandRef);
      }
    }

    private void OnTerminated(Terminated terminatedMsg)
    {
      System.Diagnostics.Debug.Assert(_userLogin is not null);

      var commandRef = terminatedMsg.ActorRef;

      if (_commandIdByShellCommandRef.TryGetValue(commandRef, out var commandId)
        && _inputOriginByCommandId.TryGetValue(commandId, out var data))
      {
        _logger.Warning($"{nameof(OnTerminated)}: unexpected stop of command {commandId}, {commandRef.Path}");
        _commandIdByShellCommandRef.Remove(commandRef);
        _inputOriginByCommandId.Remove(commandId);

        var inputErrorMsg = new ShellInputErrorMsg
        {
          UserLoginId = _userLogin.UserLoginId,
          ShellInputId = data.Input.ShellInputId,
          ExitCode = -1,
          ErrorMessage = "shell error: unexpected stop of command",
        };
        data.InputOrigin.Tell(inputErrorMsg);
      }
    }

    public static Props Props()
      => Akka.Actor.Props
        .Create<ShellActor>()
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
