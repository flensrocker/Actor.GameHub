using System;

namespace Actor.GameHub.Identity.Abstractions
{
  public static class IdentityMetadata
  {
    public static readonly string IdentityName = "Identity";
    public static readonly string IdentityPath = $"/user/{IdentityName}";

    public static string AuthenticatorName(Guid authId) => $"Authenticator-{authId}";
    public static string LoaderName(Guid loadId) => $"Loader-{loadId}";
  }
}
