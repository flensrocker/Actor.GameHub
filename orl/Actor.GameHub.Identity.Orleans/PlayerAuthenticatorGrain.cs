using System;
using System.Threading.Tasks;
using Actor.GameHub.Identity.Abstractions;
using Orleans;
using Orleans.Transactions.Abstractions;

namespace Actor.GameHub.Identity.Orleans
{
  public class PlayerAuthState
  {
    public string Name { get; set; }
    public string PasswordHash { get; set; }
    public DateTime? LastLoginAt { get; set; }
  }

  public class PlayerAuthenticatorGrain : Grain, IPlayerAuthenticator
  {
    private readonly ITransactionalState<PlayerAuthState> _authState;

    public PlayerAuthenticatorGrain([TransactionalState(IdentityExtensions.PlayerAuthStorage, IdentityExtensions.StorageName)] ITransactionalState<PlayerAuthState> authState)
    {
      _authState = authState;
    }

    public async Task Register(RegisterRequest request)
    {
      var name = await _authState.PerformRead(s => s.Name);
      if (!string.IsNullOrWhiteSpace(name))
        throw new IdentityBadRequestException("player-id collision, try again");

      await _authState.PerformUpdate(s =>
      {
        s.Name = request.Name;
        s.PasswordHash = IdentityExtensions.HashPassword(request.Password);
      });
    }

    public async Task<PasswordLoginResponse> PasswordLogin(PasswordLoginRequest request)
    {
      var player = await _authState.PerformRead(s => new
      {
        s.Name,
        s.PasswordHash,
      });
      if (!IdentityExtensions.VerifyPassword(request.Password, player.PasswordHash))
        throw new IdentityForbiddenException("password is wrong");

      await _authState.PerformUpdate(s =>
      {
        s.LastLoginAt = DateTime.UtcNow;
      });

      return new PasswordLoginResponse
      {
        PlayerId = this.GetPrimaryKey(),
        Name = player.Name,
        AuthToken = "TODO",
      };
    }

    public async Task ChangeName(ChangeNameRequest request)
    {
      if (string.IsNullOrWhiteSpace(request.NewName))
        throw new IdentityBadRequestException("name is required");

      var name = await _authState.PerformRead(s => s.Name);
      if (name == request.NewName)
        return;

      var newPlayer = GrainFactory.GetGrain<IPlayerRegistry>(request.NewName);
      await newPlayer.SetPlayerId(new SetPlayerIdRequest
      {
        PlayerId = this.GetPrimaryKey(),
      });

      await _authState.PerformUpdate(s =>
      {
        s.Name = request.NewName;
      });

      var oldPlayer = GrainFactory.GetGrain<IPlayerRegistry>(name);
      await oldPlayer.Delete();
    }

    public async Task ChangePassword(ChangePasswordRequest request)
    {
      if (!IdentityExtensions.PasswordIsValid(request.NewPassword))
        throw new IdentityBadRequestException("new password is invalid");

      var passwordHash = await _authState.PerformRead(s => s.PasswordHash);
      if (!IdentityExtensions.VerifyPassword(request.OldPassword, passwordHash))
        throw new IdentityForbiddenException("old password is invalid");

      await _authState.PerformUpdate(s =>
      {
        s.PasswordHash = IdentityExtensions.HashPassword(request.NewPassword);
      });
    }
  }
}
