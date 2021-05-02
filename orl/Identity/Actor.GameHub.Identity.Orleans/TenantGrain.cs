using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Actor.GameHub.Identity.Abstractions;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;

namespace Actor.GameHub.Identity.Orleans
{
  public class TenantState
  {
    public Guid TenantId { get; set; }
    public string ShortName { get; set; }
    public Dictionary<Guid, string> UserIdUsernameMap { get; set; }
    public Dictionary<string, Guid> UsernameUserIdMap { get; set; }
  }

  [ImplicitStreamSubscription(TenantRegistryConstants.StreamNamespace)]
  public class TenantGrain : Grain, IGrainWithGuidKey
  {
    private readonly IPersistentState<TenantState> _tenant;

    private IStreamProvider _streamProvider;

    public TenantGrain([PersistentState(nameof(TenantState), IdentityConstants.StorageProviderName)] IPersistentState<TenantState> tenant)
    {
      _tenant = tenant;
    }

    public override async Task OnActivateAsync()
    {
      _streamProvider = GetStreamProvider(IdentityConstants.StreamProviderName);

      await _streamProvider
        .GetStream<BaseTenantRegistryEvent>(this.GetPrimaryKey(), TenantRegistryConstants.StreamNamespace)
        .SubscribeAsync(OnNextTenantRegistryAsync);

      await base.OnActivateAsync();
    }

    public async Task OnNextTenantRegistryAsync(BaseTenantRegistryEvent item, StreamSequenceToken token = null)
    {
      switch (item)
      {
        case TenantCreatedEvent createdEvt when _tenant.State.TenantId == Guid.Empty:
          {
            var adminUserId = Guid.NewGuid();

            _tenant.State = new TenantState
            {
              TenantId = createdEvt.TenantId,
              ShortName = createdEvt.Command.TenantShortname,
              UserIdUsernameMap = new()
              {
                { adminUserId, createdEvt.Command.AdminUsername.ToLowerInvariant() },
              },
              UsernameUserIdMap = new()
              {
                { createdEvt.Command.AdminUsername.ToLowerInvariant(), adminUserId },
              },
            };

            await _tenant.WriteStateAsync();

            // TODO send UserCreatedEvent
            break;
          }
      }
    }
  }
}
