using System;
using System.Threading.Tasks;
using Actor.GameHub.Identity.Abtractions;
using Akka.Actor;
using Akka.Event;

namespace Actor.GameHub.Identity.Actors
{
  public class UserSessionActor : ReceiveActor
  {
    private readonly ILoggingAdapter _logger = Context.GetLogger();

    private Guid? _userId;
    private string? _username;

    public UserSessionActor()
    {
      Receive<UserLoginSuccessMsg>(OnLoginSuccess);
    }

    private void OnLoginSuccess(UserLoginSuccessMsg successMsg)
    {
      if (!_userId.HasValue)
      {
        _userId = successMsg.UserId;
        _username = successMsg.Username;
      }
      else if (_userId.Value != successMsg.UserId)
        throw new Exception($"UserId mismatch {_userId} != {successMsg.UserId}");

      Sender.Tell(successMsg);
      _logger.Info($"{nameof(OnLoginSuccess)}: {_username} logged in with userId {_userId} from {Sender.Path}");
    }

    public static Props Props()
      => Akka.Actor.Props
        .Create(() => new UserSessionActor())
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
