using System;

namespace Actor.GameHub.Identity.Abstractions
{
  public static class IdentityMetadata
  {
    public static readonly string IdentityName = "Identity";
    public static readonly string IdentityPath = $"/user/{IdentityName}";

    public static string UserLoaderName(Guid loadId) => $"UserLoader-{loadId}";
    public static string UserAuthenticatorName(Guid authId) => $"UserAuthenticator-{authId}";
  }
}
