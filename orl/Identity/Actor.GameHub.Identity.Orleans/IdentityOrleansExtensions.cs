using System.Collections.Generic;
using System.Reflection;
using Actor.GameHub.Identity.Abstractions;
using Orleans;
using Orleans.Hosting;

namespace Actor.GameHub.Identity.Orleans
{
  public static class IdentityOrleansExtensions
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
        // TenantRegistry
        .AddAzureTableGrainStorage(TenantRegistryConstants.StorageProviderName, options =>
        {
          options.UseJson = true;
          options.ConnectionString = azureStorageConnectionString;
        })
        .AddAzureQueueStreams(TenantRegistryConstants.QueueProviderName, config =>
        {
          config.ConfigureAzureQueue(queueOptions =>
          {
            queueOptions.Configure(options =>
            {
              options.ConnectionString = azureStorageConnectionString;
              options.QueueNames = new List<string> { "tenant-registry-queue-0" };
            });
          });
        })
        // Grains
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
