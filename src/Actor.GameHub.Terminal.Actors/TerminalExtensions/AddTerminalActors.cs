using Actor.GameHub.Terminal.Abstractions;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;

namespace Actor.GameHub.Terminal
{
  public static partial class TerminalExtensions
  {
    public static TActorSystem AddTerminalActors<TActorSystem>(this TActorSystem actorSystem)
      where TActorSystem : ActorSystem
    {
      var terminal = actorSystem.ActorOf(TerminalActor.Props(), TerminalMetadata.TerminalName);

      var mediator = DistributedPubSub.Get(actorSystem).Mediator;
      mediator.Tell(new Put(terminal));

      return actorSystem;
    }
  }
}
