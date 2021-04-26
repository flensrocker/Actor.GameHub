using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Actor.GameHub.Identity;
using Actor.GameHub.Identity.EntityFrameworkCore;
using Actor.GameHub.Terminal;
using Akka.Actor;
using Akka.Configuration;
using Akka.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace Actor.GameHub
{
  public class IdentityDesignTimeDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
  {
    public IdentityDbContext CreateDbContext(string[] args)
    {
      var dbOptions = new DbContextOptionsBuilder<IdentityDbContext>()
        .UseSqlServer(Program.ConnectionString, sqlOptions =>
        {
          sqlOptions.MigrationsAssembly(Assembly.GetAssembly(typeof(IdentityDbContext))!.GetName().Name);
        })
        .Options;
      return new IdentityDbContext(dbOptions);
    }
  }

  class Program
  {
    public const string ConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=ActorGameHub;Trusted_Connection=True;MultipleActiveResultSets=true";

    static async Task MigrateAsync(IServiceProvider serviceProvider)
    {
      using (var scope = serviceProvider.CreateScope())
      {
        var identityDbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IdentityDbContext>>();
        var identityDbContext = identityDbFactory.CreateDbContext();
        await identityDbContext.Database.MigrateAsync().ConfigureAwait(false);
      }
    }

    static async Task Main(string[] args)
    {
      var configFile = args is { Length: 1 } ? args[0] : "gamehub-seed.akka";
      var config = File.Exists(configFile)
        ? ConfigurationFactory.ParseString(await File.ReadAllTextAsync(configFile))
        : ConfigurationFactory.Default();

      var services = new ServiceCollection();

      services.AddIdentityEntityFrameworkCore(dbOptionBuilder =>
      {
        dbOptionBuilder.UseSqlServer(ConnectionString);
      });
      services.AddTerminalServices();

      var serviceProvider = services.BuildServiceProvider();

      await MigrateAsync(serviceProvider).ConfigureAwait(false);

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
