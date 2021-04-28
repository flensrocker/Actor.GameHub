using System.Threading.Tasks;
using Orleans;

namespace Actor.GameHub.Identity.Abstractions
{
  public interface IPlayerByUsername : IGrainWithStringKey
  {
    [Transaction(TransactionOption.Create)]
    Task<(IdentityError Error, RegisterResponse Response)> Register(RegisterRequest request);

    [Transaction(TransactionOption.Join)]
    Task<IdentityError> SetPlayerId(SetPlayerIdRequest request);
    [Transaction(TransactionOption.Join)]
    Task<IdentityError> Delete();

    [Transaction(TransactionOption.Create)]
    Task<(IdentityError Error, PasswordLoginResponse Response)> PasswordLogin(PasswordLoginRequest request);
  }
}
