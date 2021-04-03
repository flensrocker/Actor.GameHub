using System;
using System.Collections.Generic;
using Actor.GameHub.Identity.Abtractions;
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
      Receive<UserLoginSuccessMsg>(OnLoginSuccess);
      Receive<Terminated>(OnTerminated);
    }

    private void OnLoginSuccess(UserLoginSuccessMsg successMsg)
    {
      if (_user is null)
        _user = successMsg.User;
      else if (_user.UserId != successMsg.User.UserId)
        throw new Exception($"UserId mismatch {_user.UserId} != {successMsg.User.UserId}");

      var userLogin = Context.ActorOf(UserLoginActor.Props(), IdentityMetadata.UserLoginName(successMsg.UserLoginId));
      Context.Watch(userLogin);
      userLogin.Tell(successMsg);

      _loginId.Add(userLogin, successMsg.UserLoginId);

      _logger.Info($"{nameof(OnLoginSuccess)}: {_user.Username} logged in with userId {_user.UserId} from {Sender.Path}");
    }

    private void OnTerminated(Terminated terminatedMsg)
    {
      if (_loginId.Remove(terminatedMsg.ActorRef))
      {
        if (_loginId.Count == 0)
        {
          Context.System.Stop(Self);

          _logger.Info($"user session closed for user {_user?.Username}");
        }
      }
    }

    public static Props Props()
      => Akka.Actor.Props
        .Create(() => new UserSessionActor())
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
