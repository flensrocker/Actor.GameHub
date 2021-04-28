using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Actor.GameHub.Identity.AspNetCore
{
  public static partial class IdentityAspNetCoreExtensions
  {
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
      endpoints.MapPost("/api/Identity/Player/Register", RegisterPlayer);
      endpoints.MapPost("/api/Identity/Player/PasswordLogin", LoginPlayer);

      return endpoints;
    }
  }
}
