using System.Threading.Tasks;
using Orleans;

namespace Actor.GameHub.Identity.Abstractions
{
  public interface IPlayerById : IGrainWithGuidKey
  {
    [Transaction(TransactionOption.Join)]
    Task Register(RegisterRequest request);

    [Transaction(TransactionOption.Create)]
    Task ChangeName(ChangeNameRequest request);

    [Transaction(TransactionOption.Supported)]
    Task ChangePassword(ChangePasswordRequest request);

    [Transaction(TransactionOption.Supported)]
    Task<PasswordLoginResponse> PasswordLogin(PasswordLoginRequest request);
  }
}
