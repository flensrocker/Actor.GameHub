using System;
using System.Threading.Tasks;
using Actor.GameHub.Identity.Abstractions;
using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Event;
using Microsoft.Extensions.DependencyInjection;

namespace Actor.GameHub.Identity.Actors
{
  public class UserLoaderActor : ReceiveActor
  {
    private readonly ILoggingAdapter _logger = Context.GetLogger();

    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;

    private readonly IIdentityRepository _identityRepository;

    public UserLoaderActor(IServiceProvider serviceProvider)
    {
      _serviceProvider = serviceProvider;
      _scope = _serviceProvider.CreateScope();
      _identityRepository = _scope.ServiceProvider.GetRequiredService<IIdentityRepository>();

      ReceiveAsync<LoadUserByUsernameForAuthMsg>(LoadUserByUsernameAsync);
    }

    protected override void PostStop()
    {
      _scope.Dispose();
    }

    private async Task LoadUserByUsernameAsync(LoadUserByUsernameForAuthMsg loadMsg)
    {
      // save Context/Sender before await
      var loadOrigin = Sender;

      var user = await _identityRepository.FindUserByUsernameForAuthAsync(loadMsg.Username);
      object reply = user is null
        ? new UserLoadForAuthErrorMsg
        {
          LoadId = loadMsg.LoadId,
          ErrorMessage = "user not found",
        }
        : new UserLoadForAuthSuccessMsg
        {
          LoadId = loadMsg.LoadId,
          User = user,
        };

      loadOrigin.Tell(reply);
    }

    public static Props Props(ActorSystem actorSystem)
      => ServiceProvider.For(actorSystem)
        .Props<UserLoaderActor>()
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
