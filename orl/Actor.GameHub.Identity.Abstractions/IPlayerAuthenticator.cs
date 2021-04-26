using System;
using System.Threading.Tasks;
using Orleans;

namespace Actor.GameHub.Identity.Abstractions
{
  /// <summary>
  /// primary key: PlayerId
  /// </summary>
  public interface IPlayerAuthenticator : IGrainWithGuidKey
  {
    [Transaction(TransactionOption.Join)]
    Task<RegisterResponse> Register(RegisterRequest request);

    [Transaction(TransactionOption.Create)]
    Task SetName(SetNameRequest request);

    [Transaction(TransactionOption.Supported)]
    Task ChangePassword(ChangePasswordRequest request);

    [Transaction(TransactionOption.Supported)]
    Task<PasswordLoginResponse> PasswordLogin(PasswordLoginRequest request);
  }
}
