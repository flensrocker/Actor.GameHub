using System.Threading.Tasks;
using Actor.GameHub.Identity.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Orleans;

namespace Actor.GameHub.Identity.AspNetCore
{
  public static partial class IdentityAspNetCoreExtensions
  {
    public static async Task LoginPlayer(HttpContext context)
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
    }
  }
}
