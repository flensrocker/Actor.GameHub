using Actor.GameHub.Identity.Abtractions;
using Actor.GameHub.Identity.Actors;
using Akka.Actor;

namespace Actor.GameHub.Identity
{
  public static partial class IdentityExtensions
  {
    public static TActorSystem AddIdentityActors<TActorSystem>(this TActorSystem actorSystem)
      where TActorSystem : IActorRefFactory
    {
      actorSystem.ActorOf(IdentityManagerActor.Props(), IdentityMetadata.IdentityManagerName);

      return actorSystem;
    }
  }
}
