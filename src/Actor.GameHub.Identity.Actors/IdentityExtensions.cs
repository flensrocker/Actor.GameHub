using Actor.GameHub.Identity.Abtractions;
using Akka.Actor;

namespace Actor.GameHub.Identity
{
  public static class IdentityExtensions
  {
    public static TActorSystem AddIdentity<TActorSystem>(this TActorSystem actorSystem)
      where TActorSystem : IActorRefFactory
    {
      actorSystem.ActorOf(IdentityManager.Props(), IdentityMetaData.IdentityManagerName);

      return actorSystem;
    }
  }
}
