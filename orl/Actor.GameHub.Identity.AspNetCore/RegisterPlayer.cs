using System.Threading.Tasks;
using Actor.GameHub.Identity.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Orleans;

namespace Actor.GameHub.Identity.AspNetCore
{
  public static partial class IdentityAspNetCoreExtensions
  {
    public static async Task RegisterPlayer(HttpContext context)
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
    }
  }
}
