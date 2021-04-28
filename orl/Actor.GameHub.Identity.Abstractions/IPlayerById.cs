using System.Threading.Tasks;
using Orleans;

namespace Actor.GameHub.Identity.Abstractions
{
  public interface IPlayerById : IGrainWithGuidKey
  {
    [Transaction(TransactionOption.Join)]
    Task<IdentityError> Register(RegisterRequest request);

    [Transaction(TransactionOption.Create)]
    Task<IdentityError> ChangeUsername(ChangeUsernameRequest request);

    [Transaction(TransactionOption.Create)]
    Task<IdentityError> ChangePassword(ChangePasswordRequest request);

    [Transaction(TransactionOption.Join)]
    Task<(IdentityError Error, PasswordLoginResponse Response)> PasswordLogin(PasswordLoginRequest request);
  }
}
