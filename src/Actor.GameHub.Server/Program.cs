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
      var config = File.Exists("app.config")
        ? ConfigurationFactory.ParseString(await File.ReadAllTextAsync("app.config"))
        : ConfigurationFactory.Default();

      var gameHubServerSystem = ActorSystem
        .Create("GameHub", config)
        .AddIdentityActors()
        .AddTerminalActors();

      Console.ReadLine();

      await gameHubServerSystem.Terminate();
    }
  }
}
