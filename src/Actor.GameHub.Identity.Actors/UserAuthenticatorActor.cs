using System;
using System.Collections.Generic;
using Actor.GameHub.Identity.Abtractions;
using Akka.Actor;

namespace Actor.GameHub.Identity.Actors
{
  public class UserAuthenticatorActor : ReceiveActor
  {
    private readonly Dictionary<Guid, (AuthUserMsg AuthMsg, IActorRef Sender)> _authSenderByLoadId = new();
    private readonly Dictionary<IActorRef, Guid> _loadIdByUserLoader = new();

    public UserAuthenticatorActor()
    {
      Receive<AuthUserMsg>(AuthUser);
      Receive<UserLoadErrorMsg>(UserLoadError);
      Receive<UserLoadSuccessMsg>(UserLoadSuccess);
      Receive<Terminated>(OnTerminate);
    }

    private void AuthUser(AuthUserMsg authMsg)
    {
      var loadUserMsg = new LoadUserByUsernameMsg
      {
        Username = authMsg.LoginUserMsg.Username,
      };

      if (_authSenderByLoadId.TryAdd(loadUserMsg.LoadId, (authMsg, Sender)))
      {
        var userLoader = Context.ActorOf(UserLoaderActor.Props(), IdentityMetadata.UserLoaderName(loadUserMsg.LoadId));
        Context.Watch(userLoader);
        userLoader.Tell(loadUserMsg);

        _loadIdByUserLoader.Add(userLoader, loadUserMsg.LoadId);
      }
      else
      {
        var authErrorMsg = new UserAuthErrorMsg
        {
          AuthId = authMsg.AuthId,
          ErrorMessage = "user auth loadId error, try again...",
        };
        Sender.Tell(authErrorMsg);
      }
    }

    private void UserLoadError(UserLoadErrorMsg loadErrorMsg)
    {
      if (_authSenderByLoadId.Remove(loadErrorMsg.LoadId, out var data))
      {
        var authErrorMsg = new UserAuthErrorMsg
        {
          AuthId = data.AuthMsg.AuthId,
          ErrorMessage = $"user load error: {loadErrorMsg.ErrorMessage}",
        };
        data.Sender.Tell(authErrorMsg);

        _loadIdByUserLoader.Remove(Sender);
      }
    }

    private void UserLoadSuccess(UserLoadSuccessMsg loadSuccessMsg)
    {
      if (_authSenderByLoadId.Remove(loadSuccessMsg.LoadId, out var data))
      {
        var authSuccessMsg = new UserAuthSuccessMsg
        {
          AuthId = data.AuthMsg.AuthId,
          User = loadSuccessMsg.User,
        };
        data.Sender.Tell(authSuccessMsg);

        _loadIdByUserLoader.Remove(Sender);
      }
    }

    private void OnTerminate(Terminated terminated)
    {
      if (_loadIdByUserLoader.Remove(terminated.ActorRef, out var loadId)
        && _authSenderByLoadId.Remove(loadId, out var data))
      {
        var authErrorMsg = new UserAuthErrorMsg
        {
          AuthId = data.AuthMsg.AuthId,
          ErrorMessage = "user load error: unexpected",
        };
        data.Sender.Tell(authErrorMsg);
      }
    }

    public static Props Props()
      => Akka.Actor.Props
        .Create(() => new UserAuthenticatorActor())
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
