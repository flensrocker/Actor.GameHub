using System;
using System.Reflection;
using Actor.GameHub.Identity.Abstractions;
using Orleans;
using Orleans.Hosting;

namespace Actor.GameHub.Identity.Orleans
{
  public static class IdentityExtensions
  {
    public const string StorageName = "IdentityStorage";
    public const string PlayerByUsernameStorage = "PlayerByUsernameState";
    public const string PlayerByIdStorage = "PlayerByIdState";

    public static ISiloBuilder AddIdentity(this ISiloBuilder siloBuilder, string azureStorageConnectionString)
    {
      siloBuilder
        .UseTransactions()
        .AddAzureTableTransactionalStateStorage(StorageName, options =>
        {
          options.ConnectionString = azureStorageConnectionString;
        })
        .ConfigureApplicationParts(parts =>
        {
          parts.AddApplicationPart(Assembly.GetExecutingAssembly()).WithReferences();
        });
      return siloBuilder;
    }

    public static IPlayerByUsername GetPlayerByUsername(this IGrainFactory factory, string username)
      => factory.GetGrain<IPlayerByUsername>(username.ToLowerInvariant());

    public static IPlayerById GetPlayerById(this IGrainFactory factory, Guid playerId)
      => factory.GetGrain<IPlayerById>(playerId);

    public static bool PasswordIsValid(string password)
    {
      return !string.IsNullOrWhiteSpace(password);
    }

    public static string HashPassword(string password)
    {
      return password;
    }

    public static bool VerifyPassword(string password, string passwordHash)
    {
      return password == passwordHash;
    }
  }
}
