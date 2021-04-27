using Actor.GameHub.Identity.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;

namespace Actor.GameHub.Server
{
  public class Startup
  {
    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }

      app.UseRouting();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapPost("/api/Identity/PlayerRegistry/Register", async context =>
        {
          var registerRequest = await context.Request.ReadFromJsonAsync<RegisterRequest>();

          var clusterClient = context.RequestServices.GetRequiredService<IClusterClient>();
          var playerRegistry = clusterClient.GetGrain<IPlayerRegistry>(registerRequest.Name);
          var registerResponse = await playerRegistry.Register(registerRequest);

          await context.Response.WriteAsJsonAsync(registerResponse);
        });
      });
    }
  }
}
