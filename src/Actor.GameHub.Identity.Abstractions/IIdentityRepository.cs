using System.Threading;
using System.Threading.Tasks;

namespace Actor.GameHub.Identity.Abstractions
{
  public interface IIdentityRepository
  {
    Task<User?> FindUserByUsernameForAuthAsync(string username, CancellationToken cancellationToken = default);
  }
}
