using Actor.GameHub.Identity.Abstractions;
using Actor.GameHub.Identity.Actors;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;

namespace Actor.GameHub.Identity
{
  public static partial class IdentityExtensions
  {
    public static TActorSystem AddIdentityActors<TActorSystem>(this TActorSystem actorSystem)
      where TActorSystem : ActorSystem
    {
      var identity = actorSystem.ActorOf(IdentityActor.Props(), IdentityMetadata.IdentityName);

      var mediator = DistributedPubSub.Get(actorSystem).Mediator;
      mediator.Tell(new Put(identity));

      return actorSystem;
    }
  }
}
