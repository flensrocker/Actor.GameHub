using System;
using Actor.GameHub.Identity.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Actor.GameHub.Identity.EntityFrameworkCore
{
  public static class IdentityDbExtensions
  {
    public static IServiceCollection AddIdentityEntityFrameworkCore(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction)
    {
      services.AddDbContextFactory<IdentityDbContext>(optionsAction);
      services.AddScoped<IIdentityRepository, IdentityDbRepository>();

      return services;
    }
  }
}
