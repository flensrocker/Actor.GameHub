using Actor.GameHub.Identity.Abtractions;
using Akka.Actor;

namespace Actor.GameHub.Identity.Actors
{
  public class IdentityManagerActor : ReceiveActor
  {
    private readonly IActorRef _userManager;

    public IdentityManagerActor()
    {
      _userManager = Context.ActorOf(UserManagerActor.Props(), IdentityMetadata.UserManagerName);

      Receive<IUserManagerMsg>(msg => _userManager.Forward(msg));
    }

    public static Props Props()
      => Akka.Actor.Props.Create(() => new IdentityManagerActor());
  }
}
