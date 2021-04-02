using Akka.Actor;

namespace Actor.GameHub.Identity
{
  public static class IdentityExtensions
  {
    public static IActorRef AddIdentity(this IActorRefFactory actorSystem)
    {
      return actorSystem.ActorOf(IdentityManager.Props(), "Identity");
    }
  }
}
