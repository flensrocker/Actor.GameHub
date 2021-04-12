using System;
using System.IO;
using System.Threading.Tasks;
using Actor.GameHub.Identity;
using Actor.GameHub.Terminal;
using Akka.Actor;
using Akka.Configuration;
using Akka.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Actor.GameHub
{
  class Program
  {
    static async Task Main(string[] args)
    {
      var configFile = args is { Length: 1 } ? args[0] : "gamehub-seed.akka";
      var config = File.Exists(configFile)
        ? ConfigurationFactory.ParseString(await File.ReadAllTextAsync(configFile))
        : ConfigurationFactory.Default();

      var services = new ServiceCollection();
      services.AddTerminalServices();

      var serviceProvider = services.BuildServiceProvider();
      var spSetup = ServiceProviderSetup.Create(serviceProvider);

      var setup = BootstrapSetup.Create()
        .WithConfig(config)
        .And(spSetup);

      using var gameHubServerSystem = ActorSystem.Create("GameHub", setup)
        .AddIdentityActors()
        .AddTerminalActors();

      Console.ReadLine();

      await gameHubServerSystem.Terminate();
    }
  }
}
