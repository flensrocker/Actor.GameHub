using System;
using System.Threading.Tasks;
using Actor.GameHub.Identity.Abstractions;
using Orleans;
using Orleans.Transactions.Abstractions;

namespace Actor.GameHub.Identity.Orleans
{
  public class PlayerByUsernameState
  {
    public Guid PlayerId { get; set; }
  }

  public class PlayerByUsernameGrain : Grain, IPlayerByUsername
  {
    private readonly ITransactionalState<PlayerByUsernameState> _state;

    public PlayerByUsernameGrain([TransactionalState(IdentityExtensions.PlayerByUsernameStorage, IdentityExtensions.StorageName)] ITransactionalState<PlayerByUsernameState> state)
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
      var authenticator = GrainFactory.GetPlayerById(newPlayerId);
      await authenticator.Register(request);

      await _state.PerformUpdate(s =>
      {
        s.PlayerId = newPlayerId;
      });

      return new RegisterResponse
      {
        PlayerId = newPlayerId,
      };
    }

    public async Task<PasswordLoginResponse> PasswordLogin(PasswordLoginRequest request)
    {
      if (string.IsNullOrWhiteSpace(request.Password))
        throw new IdentityBadRequestException("password is missing");

      var playerId = await _state.PerformRead(s => s.PlayerId);
      if (playerId == Guid.Empty)
        throw new IdentityNotFoundException("player not found");

      var authenticator = GrainFactory.GetPlayerById(playerId);
      return await authenticator.PasswordLogin(request);
    }

    public async Task SetPlayerId(SetPlayerIdRequest request)
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
      // there is no "PerformDelete", see https://github.com/dotnet/orleans/issues/5448
      await _state.PerformUpdate(s =>
      {
        s.PlayerId = Guid.Empty;
      });
    }
  }
}
