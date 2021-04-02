using System;
using Actor.GameHub.Identity.Abtractions;
using Akka.Actor;
using Akka.Event;

namespace Actor.GameHub.Identity.Actors
{
  public class UserActor : ReceiveActor
  {
    private readonly ILoggingAdapter _logger = Context.GetLogger();

    private Guid _userId;
    private string? _username;

    public UserActor()
    {
      Receive<UserLoginSuccessMsg>(OnLogin);
    }

    private void OnLogin(UserLoginSuccessMsg successMsg)
    {
      _userId = successMsg.UserId;
      _username = successMsg.Username;
    }

    public static Props Props()
      => Akka.Actor.Props
        .Create(() => new UserActor())
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
