﻿using System;

namespace Actor.GameHub.Identity.Abtractions
{
  public static class IdentityMetadata
  {
    public static readonly string IdentityName = "Identity";
    public static readonly string IdentityPath = $"/user/{IdentityName}";

    public static readonly string UserSessionManagerName = "UserSessionManager";

    public static string UserLoaderName(Guid loadId) => $"UserLoader-{loadId}";
    public static string UserAuthenticatorName(Guid authId) => $"UserAuthenticator-{authId}";
    public static string UserSessionName(Guid userId) => $"UserSession-{userId}";
    public static string UserLoginName(Guid loginId) => $"UserLogin-{loginId}";
  }
}