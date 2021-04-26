using System.Threading.Tasks;
using Orleans;

namespace Actor.GameHub.Identity.Abstractions
{
  /// <summary>
  /// primary key: username
  /// Mapping of username to PlayerId
  /// </summary>
  public interface IPlayerRegistry : IGrainWithStringKey
  {
    [Transaction(TransactionOption.Create)]
    Task<RegisterResponse> Register(RegisterRequest request);

    [Transaction(TransactionOption.Join)]
    Task SetPlayer(SetPlayerRequest request);
    [Transaction(TransactionOption.Join)]
    Task Delete();
  }
}
