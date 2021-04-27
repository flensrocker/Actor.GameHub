using Actor.GameHub.Identity.Orleans;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Actor.GameHub.Server
{
  public class Program
  {
    public static void Main(string[] args)
    {
      CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
      Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder =>
        {
          webBuilder.UseStartup<Startup>();
        })
        .UseOrleans(siloBuilder =>
        {
          siloBuilder
            .Configure<ClusterOptions>(options =>
            {
              options.ClusterId = "Actor.GameHub";
              options.ServiceId = "Actor.GameHub";
            })
            .UseAzureStorageClustering(options =>
            {
              options.ConnectionString = "UseDevelopmentStorage=true";
            })
            .ConfigureEndpoints(siloPort: 11111, gatewayPort: 30000)
            .AddIdentity("UseDevelopmentStorage=true");
        });
  }
}
