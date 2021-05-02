using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Actor.GameHub.Identity.Abstractions;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;

namespace Actor.GameHub.Identity.Orleans
{
  public class TenantRegistryState
  {
    public HashSet<Guid> TenantIds { get; set; } = new();
  }

  public static class TenantRegistryExtensions
  {
    public static CreateTenantErrorEvent Validate(this CreateTenantCommand createCmd)
    {
      var errorMsg = new StringBuilder();

      if (string.IsNullOrWhiteSpace(createCmd.TenantShortname))
        errorMsg.AppendLine("The shortname for the tenant is required.");
      if (string.IsNullOrWhiteSpace(createCmd.AdminUsername))
        errorMsg.AppendLine("The username for the admin is required.");
      if (string.IsNullOrWhiteSpace(createCmd.AdminPassword))
        errorMsg.AppendLine("The password for the admin is required.");

      if (errorMsg.Length == 0)
        return null;

      return new CreateTenantErrorEvent
      {
        RequestId = createCmd.RequestId,
        StatusCode = 400,
        ErrorMessage = errorMsg.ToString(),
      };
    }
  }

  [ImplicitStreamSubscription(TenantRegistryConstants.StreamNamespace)]
  public class TenantRegistryGrain : Grain, IGrainWithGuidKey
  {
    private readonly IPersistentState<TenantRegistryState> _tenantRegistry;

    private IStreamProvider _streamProvider;

    public TenantRegistryGrain([PersistentState(nameof(TenantRegistryState), IdentityConstants.StorageProviderName)] IPersistentState<TenantRegistryState> tenantRegistry)
    {
      _tenantRegistry = tenantRegistry;
    }

    public override async Task OnActivateAsync()
    {
      _streamProvider = GetStreamProvider(IdentityConstants.StreamProviderName);

      await _streamProvider
        .GetStream<BaseTenantRegistryCommand>(this.GetPrimaryKey(), TenantRegistryConstants.StreamNamespace)
        .SubscribeAsync(OnNextAsync);

      await base.OnActivateAsync();
    }

    public async Task OnNextAsync(BaseTenantRegistryCommand item, StreamSequenceToken token = null)
    {
      switch (item)
      {
        case CreateTenantCommand createCmd:
          {
            BaseTenantRegistryEvent result;

            var tenantId = TenantRegistryConstants.GrainKey;
            try
            {
              result = createCmd.Validate();
              if (result is null)
              {
                tenantId = Guid.NewGuid();
                while (!_tenantRegistry.State.TenantIds.Add(tenantId))
                  tenantId = Guid.NewGuid();

                // TODO create tenant-grain for tenant
                // TODO create user-grain for admin

                await _tenantRegistry.WriteStateAsync();

                result = new TenantCreatedEvent
                {
                  RequestId = createCmd.RequestId,
                  TenantId = tenantId,
                  Command = createCmd,
                };
              }
            }
            catch (Exception ex)
            {
              result = new CreateTenantErrorEvent
              {
                RequestId = createCmd.RequestId,
                StatusCode = 500,
                ErrorMessage = ex.Message,
              };
            }

            var eventStream = _streamProvider.GetStream<BaseTenantRegistryEvent>(tenantId, TenantRegistryConstants.StreamNamespace);
            await eventStream.OnNextAsync(result);
            break;
          }
      }
    }
  }
}
