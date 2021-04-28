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

    public PlayerByUsernameGrain([TransactionalState(IdentityOrleansExtensions.PlayerByUsernameStorage, IdentityOrleansExtensions.StorageName)] ITransactionalState<PlayerByUsernameState> state)
    {
      _state = state;
    }

    public async Task<(IdentityError, RegisterResponse)> Register(RegisterRequest request)
    {
      var playerId = await _state.PerformRead(s => s.PlayerId);
      if (playerId != Guid.Empty)
        return (IdentityError.BadRequest("name already taken"), null);

      if (!IdentityOrleansExtensions.PasswordIsValid(request.Password))
        return (IdentityError.BadRequest("password is invalid"), null);

      var newPlayerId = Guid.NewGuid();
      var authenticator = GrainFactory.GetPlayerById(newPlayerId);
      var error = await authenticator.Register(request);
      if (error is not null)
        return (error, null);

      await _state.PerformUpdate(s =>
      {
        s.PlayerId = newPlayerId;
      });

      return (null, new RegisterResponse
      {
        PlayerId = newPlayerId,
      });
    }

    public async Task<(IdentityError, PasswordLoginResponse)> PasswordLogin(PasswordLoginRequest request)
    {
      if (string.IsNullOrWhiteSpace(request.Password))
        return (IdentityError.BadRequest("password is missing"), null);

      var playerId = await _state.PerformRead(s => s.PlayerId);
      if (playerId == Guid.Empty)
        return (IdentityError.NotFound("player not found"), null);

      var authenticator = GrainFactory.GetPlayerById(playerId);
      return await authenticator.PasswordLogin(request);
    }

    public async Task<IdentityError> SetPlayerId(SetPlayerIdRequest request)
    {
      var playerId = await _state.PerformRead(s => s.PlayerId);
      if (playerId != Guid.Empty)
        return IdentityError.BadRequest("name already taken");

      await _state.PerformUpdate(s =>
      {
        s.PlayerId = request.PlayerId;
      });

      return null;
    }

    public async Task<IdentityError> Delete()
    {
      // there is no "PerformDelete", see https://github.com/dotnet/orleans/issues/5448
      await _state.PerformUpdate(s =>
      {
        s.PlayerId = Guid.Empty;
      });

      return null;
    }
  }
}
