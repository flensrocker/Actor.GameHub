using System;
using System.Threading.Tasks;
using Actor.GameHub.Identity.Abstractions;
using Orleans;
using Orleans.Runtime;
using Orleans.Transactions.Abstractions;

namespace Actor.GameHub.Identity.Orleans
{
  public class PlayerRegistryState
  {
    public Guid PlayerId { get; set; }
  }

  public class PlayerRegistryGrain : Grain, IPlayerRegistry
  {
    private readonly ITransactionalState<PlayerRegistryState> _state;

    public PlayerRegistryGrain([TransactionalState(IdentityExtensions.PlayerRegistryStorage, IdentityExtensions.StorageName)] ITransactionalState<PlayerRegistryState> state)
    {
      _state = state;
    }

    public async Task<RegisterResponse> Register(RegisterRequest request)
    {
      var playerId = await _state.PerformRead(s => s.PlayerId);
      if (playerId != Guid.Empty)
        throw new IdentityBadRequestException("name already taken");

      if (!IdentityExtensions.PasswordIsValid(request.Password))
        throw new IdentityBadRequestException("password is invalid");

      var newPlayerId = Guid.NewGuid();
      var authenticator = GrainFactory.GetGrain<IPlayerAuthenticator>(newPlayerId);
      var response = await authenticator.Register(request);

      await _state.PerformUpdate(s =>
      {
        s.PlayerId = newPlayerId;
      });

      return response;
    }

    public async Task SetPlayer(SetPlayerRequest request)
    {
      var playerId = await _state.PerformRead(s => s.PlayerId);
      if (playerId != Guid.Empty)
        throw new IdentityBadRequestException("name already taken");

      await _state.PerformUpdate(s =>
      {
        s.PlayerId = request.PlayerId;
      });
    }

    public async Task Delete()
    {
      await _state.PerformUpdate(s =>
      {
        s.PlayerId = Guid.Empty;
      });
    }
  }
}
