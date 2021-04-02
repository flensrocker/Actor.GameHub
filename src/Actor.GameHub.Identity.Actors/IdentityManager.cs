using Actor.GameHub.Identity.Abtractions;
using Akka.Actor;

namespace Actor.GameHub.Identity
{
  public class IdentityManager : ReceiveActor
  {
    private readonly IActorRef _userManager;

    public IdentityManager()
    {
      _userManager = Context.ActorOf(UserManager.Props(), "UserManager");

      Receive<IUserManagerMsg>(msg => _userManager.Forward(msg));
    }

    public static Props Props()
      => Akka.Actor.Props.Create(() => new IdentityManager());
  }
}
