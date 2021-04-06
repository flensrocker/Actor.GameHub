using System.Threading.Tasks;
using Actor.GameHub.Identity.Abstractions;
using Akka.Actor;
using Akka.Event;

namespace Actor.GameHub.Identity.Actors
{
  public class UserLoaderActor : ReceiveActor
  {
    private readonly ILoggingAdapter _logger = Context.GetLogger();

    private readonly IIdentityRepository _identityRepository = new DummyIdentityRepository();

    public UserLoaderActor()
    {
      ReceiveAsync<LoadUserByUsernameForAuthMsg>(LoadUserByUsernameAsync);
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

    public static Props Props()
      => Akka.Actor.Props
        .Create(() => new UserLoaderActor())
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
