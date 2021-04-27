using Actor.GameHub.Identity.Abstractions;
using Actor.GameHub.Identity.Orleans;
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
        endpoints.MapPost("/api/Identity/Player/Register", async context =>
        {
          try
          {
            var registerRequest = await context.Request.ReadFromJsonAsync<RegisterRequest>();

            var clusterClient = context.RequestServices.GetRequiredService<IClusterClient>();
            var playerRegistry = clusterClient.GetPlayerByUsername(registerRequest.Name);
            var registerResponse = await playerRegistry.Register(registerRequest);
            await context.Response.WriteAsJsonAsync(registerResponse);
          }
          catch (IdentityException iex)
          {
            context.Response.StatusCode = iex.StatusCode;
            await context.Response.WriteAsJsonAsync(new { ErrorMessage = iex.Message });
          }
        });
      });
    }
  }
}
