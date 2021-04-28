using System;
using System.Threading.Tasks;
using Actor.GameHub.Identity.Abstractions;
using Orleans;
using Orleans.Transactions.Abstractions;

namespace Actor.GameHub.Identity.Orleans
{
  public class PlayerByIdState
  {
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public DateTime? LastLoginAt { get; set; }
  }

  public class PlayerByIdGrain : Grain, IPlayerById
  {
    private readonly ITransactionalState<PlayerByIdState> _state;

    public PlayerByIdGrain([TransactionalState(IdentityOrleansExtensions.PlayerByIdStorage, IdentityOrleansExtensions.StorageName)] ITransactionalState<PlayerByIdState> authState)
    {
      _state = authState;
    }

    public async Task<IdentityError> Register(RegisterRequest request)
    {
      var name = await _state.PerformRead(s => s.Username);
      if (!string.IsNullOrWhiteSpace(name))
        return IdentityError.BadRequest("player-id collision, try again");

      await _state.PerformUpdate(s =>
      {
        s.Username = request.Username;
        s.PasswordHash = IdentityOrleansExtensions.HashPassword(request.Password);
      });

      return null;
    }

    public async Task<(IdentityError, PasswordLoginResponse)> PasswordLogin(PasswordLoginRequest request)
    {
      var player = await _state.PerformRead(s => new
      {
        s.Username,
        s.PasswordHash,
      });
      if (!IdentityOrleansExtensions.VerifyPassword(request.Password, player.PasswordHash))
        return (IdentityError.Forbidden("password is wrong"), null);

      await _state.PerformUpdate(s =>
      {
        s.LastLoginAt = DateTime.UtcNow;
      });

      return (null, new PasswordLoginResponse
      {
        PlayerId = this.GetPrimaryKey(),
        Username = player.Username,
        AuthToken = "TODO",
      });
    }

    public async Task<IdentityError> ChangeUsername(ChangeUsernameRequest request)
    {
      if (string.IsNullOrWhiteSpace(request.NewUsername))
        return IdentityError.BadRequest("name is required");

      var name = await _state.PerformRead(s => s.Username);
      if (name == request.NewUsername)
        return null;

      var newPlayer = GrainFactory.GetPlayerByUsername(request.NewUsername);
      var error = await newPlayer.SetPlayerId(new SetPlayerIdRequest
      {
        PlayerId = this.GetPrimaryKey(),
      });
      if (error is not null)
        return error;

      await _state.PerformUpdate(s =>
      {
        s.Username = request.NewUsername;
      });

      var oldPlayer = GrainFactory.GetPlayerByUsername(name);
      return await oldPlayer.Delete();
    }

    public async Task<IdentityError> ChangePassword(ChangePasswordRequest request)
    {
      if (!IdentityOrleansExtensions.PasswordIsValid(request.NewPassword))
        return IdentityError.BadRequest("new password is invalid");

      var passwordHash = await _state.PerformRead(s => s.PasswordHash);
      if (!IdentityOrleansExtensions.VerifyPassword(request.OldPassword, passwordHash))
        return IdentityError.Forbidden("old password is invalid");

      await _state.PerformUpdate(s =>
      {
        s.PasswordHash = IdentityOrleansExtensions.HashPassword(request.NewPassword);
      });

      return null;
    }
  }
}
