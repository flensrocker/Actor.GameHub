using System;
using System.Threading.Tasks;
using Actor.GameHub.Identity.Abstractions;
using Orleans;
using Orleans.Transactions.Abstractions;

namespace Actor.GameHub.Identity.Orleans
{
  public class PlayerByIdState
  {
    public string Name { get; set; }
    public string PasswordHash { get; set; }
    public DateTime? LastLoginAt { get; set; }
  }

  public class PlayerByIdGrain : Grain, IPlayerById
  {
    private readonly ITransactionalState<PlayerByIdState> _state;

    public PlayerByIdGrain([TransactionalState(IdentityExtensions.PlayerByIdStorage, IdentityExtensions.StorageName)] ITransactionalState<PlayerByIdState> authState)
    {
      _state = authState;
    }

    public async Task<IdentityError> Register(RegisterRequest request)
    {
      var name = await _state.PerformRead(s => s.Name);
      if (!string.IsNullOrWhiteSpace(name))
        return IdentityError.BadRequest("player-id collision, try again");

      await _state.PerformUpdate(s =>
      {
        s.Name = request.Name;
        s.PasswordHash = IdentityExtensions.HashPassword(request.Password);
      });

      return null;
    }

    public async Task<(IdentityError, PasswordLoginResponse)> PasswordLogin(PasswordLoginRequest request)
    {
      var player = await _state.PerformRead(s => new
      {
        s.Name,
        s.PasswordHash,
      });
      if (!IdentityExtensions.VerifyPassword(request.Password, player.PasswordHash))
        return (IdentityError.Forbidden("password is wrong"), null);

      await _state.PerformUpdate(s =>
      {
        s.LastLoginAt = DateTime.UtcNow;
      });

      return (null, new PasswordLoginResponse
      {
        PlayerId = this.GetPrimaryKey(),
        Name = player.Name,
        AuthToken = "TODO",
      });
    }

    public async Task<IdentityError> ChangeName(ChangeNameRequest request)
    {
      if (string.IsNullOrWhiteSpace(request.NewName))
        return IdentityError.BadRequest("name is required");

      var name = await _state.PerformRead(s => s.Name);
      if (name == request.NewName)
        return null;

      var newPlayer = GrainFactory.GetPlayerByUsername(request.NewName);
      var error = await newPlayer.SetPlayerId(new SetPlayerIdRequest
      {
        PlayerId = this.GetPrimaryKey(),
      });
      if (error is not null)
        return error;

      await _state.PerformUpdate(s =>
      {
        s.Name = request.NewName;
      });

      var oldPlayer = GrainFactory.GetPlayerByUsername(name);
      return await oldPlayer.Delete();
    }

    public async Task<IdentityError> ChangePassword(ChangePasswordRequest request)
    {
      if (!IdentityExtensions.PasswordIsValid(request.NewPassword))
        return IdentityError.BadRequest("new password is invalid");

      var passwordHash = await _state.PerformRead(s => s.PasswordHash);
      if (!IdentityExtensions.VerifyPassword(request.OldPassword, passwordHash))
        return IdentityError.Forbidden("old password is invalid");

      await _state.PerformUpdate(s =>
      {
        s.PasswordHash = IdentityExtensions.HashPassword(request.NewPassword);
      });

      return null;
    }
  }
}
