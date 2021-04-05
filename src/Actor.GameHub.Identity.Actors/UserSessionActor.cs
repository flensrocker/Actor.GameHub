using System;
using System.Collections.Generic;
using Actor.GameHub.Identity.Abstractions;
using Akka.Actor;
using Akka.Event;

namespace Actor.GameHub.Identity.Actors
{
  public class UserSessionActor : ReceiveActor
  {
    private readonly ILoggingAdapter _logger = Context.GetLogger();

    private User? _user;
    private readonly Dictionary<IActorRef, Guid> _loginId = new();

    public UserSessionActor()
    {
      Receive<AddUserLoginMsg>(AddUserLogin);
      Receive<Terminated>(OnTerminated);
    }

    private void AddUserLogin(AddUserLoginMsg addLoginMsg)
    {
      if (_user is null)
        _user = addLoginMsg.User;
      else if (_user.UserId != addLoginMsg.User.UserId)
        throw new Exception($"UserId mismatch {_user.UserId} != {addLoginMsg.User.UserId}");

      var shell = Context.ActorOf(ShellActor.Props(), IdentityMetadata.ShellName(addLoginMsg.UserLoginId));
      _loginId.Add(shell, addLoginMsg.UserLoginId);
      Context.Watch(shell);
      shell.Tell(addLoginMsg);
    }

    private void OnTerminated(Terminated terminatedMsg)
    {
      if (_loginId.Remove(terminatedMsg.ActorRef)
        && _loginId.Count == 0)
      {
        Context.System.Stop(Self);
        _logger.Info($"user session closed for user {_user?.Username}");
      }
    }

    public static Props Props()
      => Akka.Actor.Props
        .Create(() => new UserSessionActor())
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
