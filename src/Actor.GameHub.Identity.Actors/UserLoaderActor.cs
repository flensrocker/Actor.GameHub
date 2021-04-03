using Actor.GameHub.Identity.Abtractions;
using Akka.Actor;

namespace Actor.GameHub.Identity.Actors
{
  public class UserLoaderActor : ReceiveActor
  {
    private readonly UserRepository _userRepository = new();

    public UserLoaderActor()
    {
      Receive<LoadUserByUsernameMsg>(LoadUserByUsername);
    }

    private void LoadUserByUsername(LoadUserByUsernameMsg loadMsg)
    {
      var user = _userRepository.FindByUsername(loadMsg.Username);
      object reply = user is null
        ? new UserLoadErrorMsg
        {
          LoadId = loadMsg.LoadId,
          ErrorMessage = "user not found",
        }
        : new UserLoadSuccessMsg
        {
          LoadId = loadMsg.LoadId,
          User = user,
        };

      Sender.Tell(reply);
    }

    public static Props Props()
      => Akka.Actor.Props
        .Create(() => new UserLoaderActor())
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
