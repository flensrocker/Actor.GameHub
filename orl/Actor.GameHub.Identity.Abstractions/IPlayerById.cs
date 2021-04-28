using System.Threading.Tasks;
using Orleans;

namespace Actor.GameHub.Identity.Abstractions
{
  public interface IPlayerById : IGrainWithGuidKey
  {
    [Transaction(TransactionOption.Join)]
    Task<IdentityError> Register(RegisterRequest request);

    [Transaction(TransactionOption.Create)]
    Task<IdentityError> ChangeName(ChangeNameRequest request);

    [Transaction(TransactionOption.Supported)]
    Task<IdentityError> ChangePassword(ChangePasswordRequest request);

    [Transaction(TransactionOption.Supported)]
    Task<(IdentityError Error, PasswordLoginResponse Response)> PasswordLogin(PasswordLoginRequest request);
  }
}
