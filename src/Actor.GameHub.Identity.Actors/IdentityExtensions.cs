using Actor.GameHub.Identity.Abtractions;
using Akka.Actor;

namespace Actor.GameHub.Identity.Actors
{
  public static class IdentityExtensions
  {
    public static TActorSystem AddIdentityActors<TActorSystem>(this TActorSystem actorSystem)
      where TActorSystem : IActorRefFactory
    {
      actorSystem.ActorOf(IdentityManagerActor.Props(), IdentityMetadata.IdentityManagerName);

      return actorSystem;
    }
  }
}
