using System.Threading.Tasks;
using Orleans;

namespace Actor.GameHub.Identity.Abstractions
{
  public interface IPlayerByUsername : IGrainWithStringKey
  {
    [Transaction(TransactionOption.Create)]
    Task<RegisterResponse> Register(RegisterRequest request);

    [Transaction(TransactionOption.Join)]
    Task SetPlayerId(SetPlayerIdRequest request);
    [Transaction(TransactionOption.Join)]
    Task Delete();

    [Transaction(TransactionOption.Supported)]
    Task<PasswordLoginResponse> PasswordLogin(PasswordLoginRequest request);
  }
}
