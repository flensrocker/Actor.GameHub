using System;
using System.Threading;
using System.Threading.Tasks;

namespace Actor.GameHub.Identity.Abstractions
{
  public class UserForAuth
  {
    public Guid UserId { get; init; }
    public string Username { get; init; } = null!;
    //public string PasswordHash { get; init; } = null!;
  }

  public interface IIdentityRepository
  {
    Task<UserForAuth?> FindUserByUsernameForAuthAsync(string username, CancellationToken cancellationToken = default);
  }
}
