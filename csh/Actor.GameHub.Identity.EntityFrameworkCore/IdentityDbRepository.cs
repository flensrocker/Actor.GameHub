using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Actor.GameHub.Identity.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Actor.GameHub.Identity.EntityFrameworkCore
{
  public class IdentityDbRepository : IIdentityRepository
  {
    private readonly IDbContextFactory<IdentityDbContext> _dbContextFactory;

    public IdentityDbRepository(IDbContextFactory<IdentityDbContext> dbContextFactory)
    {
      _dbContextFactory = dbContextFactory;
    }

    public async Task<UserForAuth?> FindUserByUsernameForAuthAsync(string username, CancellationToken cancellationToken = default)
    {
      var dbContext = _dbContextFactory.CreateDbContext();

      var user = await dbContext.User
        .Where(u => u.Username == username)
        .Select(u => new UserForAuth
        {
          UserId = u.Id,
          Username = u.Username,
        })
        .SingleOrDefaultAsync(cancellationToken)
        .ConfigureAwait(false);

      return user;
    }
  }
}
