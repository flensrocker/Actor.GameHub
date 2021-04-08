using System;
using System.IO;
using System.Threading.Tasks;
using Actor.GameHub.Identity;
using Actor.GameHub.Terminal;
using Akka.Actor;
using Akka.Configuration;

namespace Actor.GameHub
{
  class Program
  {
    static async Task Main(string[] args)
    {
      var configFile = args is { Length: 1 } ? args[0] : "app.config";
      var config = File.Exists(configFile)
        ? ConfigurationFactory.ParseString(await File.ReadAllTextAsync(configFile))
        : ConfigurationFactory.Default();

      var gameHubServerSystem = ActorSystem.Create("GameHub", config)
        .AddIdentityActors()
        .AddTerminalActors();

      Console.ReadLine();

      await gameHubServerSystem.Terminate();
    }
  }
}
