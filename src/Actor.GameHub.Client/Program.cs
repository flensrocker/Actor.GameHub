using System;
using System.IO;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;

namespace Actor.GameHub.Client
{
  class Program
  {
    static async Task Main(string[] args)
    {
      var configFile = args is { Length: 1 } ? args[0] : "gamehub-client.akka";
      var config = File.Exists(configFile)
        ? ConfigurationFactory.ParseString(await File.ReadAllTextAsync(configFile))
        : ConfigurationFactory.Default();

      using var gameHubClientSystem = ActorSystem.Create("GameHubClient", config);

      var consoleRef = gameHubClientSystem.ActorOf(ConsoleActor.Props(), "Console");

      do
      {
        var input = Console.ReadLine();
        if (input is null)
          break;

        var inputMsg = new InputConsoleMsg
        {
          Input = input,
        };
        consoleRef.Tell(inputMsg);
        if (input == "quit")
          break;
      } while (true);

      await gameHubClientSystem.Terminate();
    }
  }
}
