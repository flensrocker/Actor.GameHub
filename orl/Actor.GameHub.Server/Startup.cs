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
          var registerRequest = await context.Request.ReadFromJsonAsync<RegisterRequest>();

          var clusterClient = context.RequestServices.GetRequiredService<IClusterClient>();
          var playerRegistry = clusterClient.GetPlayerByUsername(registerRequest.Username);
          var (error, response) = await playerRegistry.Register(registerRequest);
          if (error is not null)
          {
            context.Response.StatusCode = error.StatusCode;
            await context.Response.WriteAsJsonAsync(new { ErrorMessage = error.Message });
          }
          else
          {
            await context.Response.WriteAsJsonAsync(response);
          }
        });

        endpoints.MapPost("/api/Identity/Player/PasswordLogin", async context =>
        {
          var loginRequest = await context.Request.ReadFromJsonAsync<PasswordLoginRequest>();

          var clusterClient = context.RequestServices.GetRequiredService<IClusterClient>();
          var playerRegistry = clusterClient.GetPlayerByUsername(loginRequest.Username);
          var (error, response) = await playerRegistry.PasswordLogin(loginRequest);
          if (error is not null)
          {
            context.Response.StatusCode = error.StatusCode;
            await context.Response.WriteAsJsonAsync(new { ErrorMessage = error.Message });
          }
          else
          {
            await context.Response.WriteAsJsonAsync(response);
          }
        });
      });
    }
  }
}
