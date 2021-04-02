using System;
using Akka.Actor;
using Akka.Event;

namespace Actor.GameHub.Identity.Actors
{
  public class UserActor : ReceiveActor
  {
    private readonly ILoggingAdapter _logger = Context.GetLogger();

    private readonly Guid _userId;
    private readonly string _username;

    public UserActor(Guid userId, string username)
    {
      _userId = userId;
      _username = username;
    }

    public static Props Props(Guid userId, string username)
      => Akka.Actor.Props.Create(() => new UserActor(userId, username));
  }
}
