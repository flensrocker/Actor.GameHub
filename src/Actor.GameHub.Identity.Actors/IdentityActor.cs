using Actor.GameHub.Identity.Abtractions;
using Akka.Actor;

namespace Actor.GameHub.Identity.Actors
{
  public class IdentityActor : ReceiveActor
  {
    private readonly IActorRef _userSessionManager;

    public IdentityActor()
    {
      _userSessionManager = Context.ActorOf(UserSessionManagerActor.Props(), IdentityMetadata.UserSessionManagerName);

      Receive<LoginUserMsg>(msg => _userSessionManager.Forward(msg));
    }

    public static Props Props()
      => Akka.Actor.Props.Create(() => new IdentityActor());
  }
}
