using Actor.GameHub.Identity.Abstractions;
using Akka.Actor;
using Akka.Event;

namespace Actor.GameHub.Identity.Actors
{
  public class IdentityActor : ReceiveActor
  {
    private readonly ILoggingAdapter _logger = Context.GetLogger();

    private readonly IActorRef _userSessionManager;

    public IdentityActor()
    {
      _userSessionManager = Context.ActorOf(UserSessionManagerActor.Props(), IdentityMetadata.UserSessionManagerName);

      Receive<LoginUserMsg>(_userSessionManager.Forward);

      _logger.Info("==> Identity started");
    }

    public static Props Props()
      => Akka.Actor.Props.Create<IdentityActor>();
  }
}
