﻿using System.Reflection;
using Orleans;
using Orleans.Hosting;

namespace Actor.GameHub.Identity.Orleans
{
  public static class IdentityExtensions
  {
    public const string StorageName = "IdentityStorage";
    public const string PlayerRegistryStorage = "PlayerRegistryState";
    public const string PlayerAuthStorage = "PlayerAuthState";

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
