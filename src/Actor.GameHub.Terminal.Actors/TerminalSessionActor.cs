using System;
using System.Collections.Generic;
using Actor.GameHub.Identity.Abstractions;
using Actor.GameHub.Terminal.Abstractions;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.DependencyInjection;
using Akka.Event;
using Microsoft.Extensions.DependencyInjection;

namespace Actor.GameHub.Terminal
{
  public class TerminalSessionActor : ReceiveActor
  {
    private readonly ILoggingAdapter _logger = Context.GetLogger();

    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly TerminalCommandService _commandService;

    private Guid _terminalId;
    private IActorRef? _terminalOrigin;
    private UserLoginSuccessMsg? _userLogin;

    private readonly Dictionary<Guid, (InputTerminalMsg Input, IActorRef InputOrigin)> _inputOriginByCommandId = new();
    private readonly Dictionary<IActorRef, Guid> _commandIdByCommandRef = new();

    public TerminalSessionActor(IServiceProvider serviceProvider)
    {
      _serviceProvider = serviceProvider;
      _scope = _serviceProvider.CreateScope();
      _commandService = _scope.ServiceProvider.GetRequiredService<TerminalCommandService>();

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
      Receive<TerminalCommandErrorMsg>(CommandError);
      Receive<TerminalCommandSuccessMsg>(CommandSuccess);
      Receive<CloseTerminalMsg>(msg => msg.TerminalId == _terminalId, Close);
      Receive<Terminated>(OnTerminated);
    }

    protected override void PostStop()
    {
      _scope.Dispose();
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

      Context.Stop(Self);
    }

    private void LoginSuccess(UserLoginSuccessMsg loginSuccessMsg)
    {
      System.Diagnostics.Debug.Assert(_terminalOrigin is not null && _userLogin is null);

      _userLogin = loginSuccessMsg;

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

      var commandProps = _commandService.Props(inputTerminalMsg.Command);
      if (commandProps is null)
      {
        var terminalErrorMsg = new TerminalInputErrorMsg
        {
          TerminalId = _terminalId,
          TerminalInputId = inputTerminalMsg.TerminalInputId,
          ExitCode = 404,
          ErrorMessage = $"Command not found: {inputTerminalMsg.Command}",
        };
        Sender.Tell(terminalErrorMsg);
      }
      else
      {
        var commandMsg = new ExecuteTerminalCommandMsg
        {
          CommandId = Guid.NewGuid(),
          Input = inputTerminalMsg,
        };

        if (_inputOriginByCommandId.TryAdd(commandMsg.CommandId, (inputTerminalMsg, Sender)))
        {
          var commandRef = Context.ActorOf(commandProps, TerminalMetadata.TerminalCommandName(commandMsg.CommandId));
          _commandIdByCommandRef.Add(commandRef, commandMsg.CommandId);

          Context.Watch(commandRef);
          commandRef.Tell(commandMsg);
        }
        else
        {
          var terminalErrorMsg = new TerminalInputErrorMsg
          {
            TerminalId = _terminalId,
            TerminalInputId = inputTerminalMsg.TerminalInputId,
            ExitCode = 500,
            ErrorMessage = "commandId error, try again...",
          };
          Sender.Tell(terminalErrorMsg);
        }
      }
    }

    private void CommandError(TerminalCommandErrorMsg commandErrorMsg)
    {
      var commandRef = Sender;

      if (_inputOriginByCommandId.TryGetValue(commandErrorMsg.CommandId, out var data)
        && _commandIdByCommandRef.ContainsKey(commandRef))
      {
        var terminalErrorMsg = new TerminalInputErrorMsg
        {
          TerminalId = _terminalId,
          TerminalInputId = data.Input.TerminalInputId,
          ExitCode = commandErrorMsg.ExitCode,
          ErrorMessage = commandErrorMsg.ErrorMessage,
        };
        data.InputOrigin.Tell(terminalErrorMsg);

        _inputOriginByCommandId.Remove(commandErrorMsg.CommandId);
        _commandIdByCommandRef.Remove(commandRef);

        Context.Unwatch(commandRef);
        Context.Stop(commandRef);
      }
    }

    private void CommandSuccess(TerminalCommandSuccessMsg commandSuccessMsg)
    {
      var commandRef = Sender;

      if (_inputOriginByCommandId.TryGetValue(commandSuccessMsg.CommandId, out var data)
        && _commandIdByCommandRef.ContainsKey(commandRef))
      {
        var terminalSuccessMsg = new TerminalInputSuccessMsg
        {
          TerminalId = _terminalId,
          TerminalInputId = data.Input.TerminalInputId,
          ExitCode = commandSuccessMsg.ExitCode,
          Output = commandSuccessMsg.Output,
        };
        data.InputOrigin.Tell(terminalSuccessMsg);

        _inputOriginByCommandId.Remove(commandSuccessMsg.CommandId);
        _commandIdByCommandRef.Remove(commandRef);

        Context.Unwatch(commandRef);
        Context.Stop(commandRef);
      }
    }

    private void Close(CloseTerminalMsg closeMsg)
    {
      System.Diagnostics.Debug.Assert(_userLogin is not null);

      var commandRef = Sender;

      if (closeMsg.CommandId.HasValue)
      {
        if (_inputOriginByCommandId.TryGetValue(closeMsg.CommandId.Value, out var data)
          && _commandIdByCommandRef.ContainsKey(commandRef))
        {
          var terminalClosedMsg = new TerminalClosedMsg
          {
            TerminalId = _terminalId,
            CommandId = closeMsg.CommandId,
            ExitCode = 0,
          };
          data.InputOrigin.Tell(terminalClosedMsg);

          _inputOriginByCommandId.Remove(closeMsg.CommandId.Value);
          _commandIdByCommandRef.Remove(commandRef);

          Context.Unwatch(commandRef);
          Context.Stop(commandRef);
        }
      }

      foreach (var cmd in _commandIdByCommandRef)
      {
        if (_inputOriginByCommandId.TryGetValue(cmd.Value, out var data))
        {
          Context.Unwatch(cmd.Key);
          Context.Stop(cmd.Key);

          var terminalErrorMsg = new TerminalInputErrorMsg
          {
            TerminalId = _terminalId,
            TerminalInputId = data.Input.TerminalInputId,
            ExitCode = -1,
            ErrorMessage = "Command cancelled",
          };
          data.InputOrigin.Tell(terminalErrorMsg);
        }
      }
      _commandIdByCommandRef.Clear();
      _inputOriginByCommandId.Clear();

      Context.Parent.Tell(new TerminalClosedMsg
      {
        TerminalId = _terminalId,
        CommandId = closeMsg.CommandId,
        ExitCode = 0,
      });
      Context.Stop(Self);
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

        var inputErrorMsg = new TerminalInputErrorMsg
        {
          TerminalId = _terminalId,
          TerminalInputId = data.Input.TerminalInputId,
          ExitCode = -1,
          ErrorMessage = $"Command error: unexpected stop of command {data.Input.Command}",
        };
        data.InputOrigin.Tell(inputErrorMsg);
      }
    }

    public static Props Props(ActorSystem actorSystem)
      => ServiceProvider.For(actorSystem)
        .Props<TerminalSessionActor>()
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
