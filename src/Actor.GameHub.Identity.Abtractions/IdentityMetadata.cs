namespace Actor.GameHub.Identity.Abtractions
{
  public static class IdentityMetadata
  {
    public static readonly string IdentityManagerName = "Identity";
    public static readonly string IdentityManagerPath = $"/user/{IdentityManagerName}";

    public static readonly string UserManagerName = "UserManager";
    public static readonly string UserManagerPath = $"{IdentityManagerPath}/{UserManagerName}";
  }
}
