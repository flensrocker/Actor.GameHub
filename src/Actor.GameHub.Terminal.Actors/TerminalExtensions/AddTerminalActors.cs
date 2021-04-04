using Actor.GameHub.Terminal.Abtractions;
using Akka.Actor;

namespace Actor.GameHub.Terminal
{
  public static partial class TerminalExtensions
  {
    public static TActorSystem AddTerminalActors<TActorSystem>(this TActorSystem actorSystem)
      where TActorSystem : IActorRefFactory
    {
      actorSystem.ActorOf(TerminalActor.Props(), TerminalMetadata.TerminalName);

      return actorSystem;
    }
  }
}
