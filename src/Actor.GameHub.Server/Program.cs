using System;
using System.IO;
using System.Threading.Tasks;
using Actor.GameHub.Commands;
using Actor.GameHub.Identity;
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

      var gamehubSystem = ActorSystem.Create("GameHub", config);

      await gamehubSystem
        .AddIdentity()
        .CommandLoopAsync(
          msg => { Console.Write(msg); return Task.CompletedTask; },
          msg => { Console.Error.Write(msg); return Task.CompletedTask; },
          () => Task.FromResult(Console.ReadLine()));

      await gamehubSystem.Terminate();
    }
  }
}
