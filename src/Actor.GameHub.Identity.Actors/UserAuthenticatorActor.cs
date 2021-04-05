using System;
using System.Collections.Generic;
using Actor.GameHub.Identity.Abstractions;
using Akka.Actor;
using Akka.Event;

namespace Actor.GameHub.Identity.Actors
{
  public class UserAuthenticatorActor : ReceiveActor
  {
    private readonly ILoggingAdapter _logger = Context.GetLogger();

    private readonly Dictionary<Guid, (AuthUserMsg AuthMsg, IActorRef AuthOrigin)> _authOriginByLoadId = new();
    private readonly Dictionary<IActorRef, Guid> _loadIdByUserLoader = new();

    public UserAuthenticatorActor()
    {
      Receive<AuthUserMsg>(AuthUser);
      Receive<UserLoadErrorMsg>(UserLoadError);
      Receive<UserLoadForAuthSuccessMsg>(UserLoadSuccess);
      Receive<Terminated>(OnTerminated);
    }

    private void AuthUser(AuthUserMsg authMsg)
    {
      var loadUserMsg = new LoadUserByUsernameForAuthMsg
      {
        Username = authMsg.LoginUserMsg.Username,
      };

      if (_authOriginByLoadId.TryAdd(loadUserMsg.LoadId, (authMsg, Sender)))
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
      if (_authOriginByLoadId.TryGetValue(loadErrorMsg.LoadId, out var data)
        && _loadIdByUserLoader.ContainsKey(Sender))
      {
        var authErrorMsg = new UserAuthErrorMsg
        {
          AuthId = data.AuthMsg.AuthId,
          ErrorMessage = $"user load error: {loadErrorMsg.ErrorMessage}",
        };
        data.AuthOrigin.Tell(authErrorMsg);

        _authOriginByLoadId.Remove(loadErrorMsg.LoadId);
        _loadIdByUserLoader.Remove(Sender);
        Context.Stop(Sender);
      }
    }

    private void UserLoadSuccess(UserLoadForAuthSuccessMsg loadSuccessMsg)
    {
      if (_authOriginByLoadId.TryGetValue(loadSuccessMsg.LoadId, out var data)
        && _loadIdByUserLoader.ContainsKey(Sender))
      {
        // TODO do authentication based on password/idToken

        var authSuccessMsg = new UserAuthSuccessMsg
        {
          AuthId = data.AuthMsg.AuthId,
          User = loadSuccessMsg.User,
        };
        data.AuthOrigin.Tell(authSuccessMsg);

        _authOriginByLoadId.Remove(loadSuccessMsg.LoadId);
        _loadIdByUserLoader.Remove(Sender);
        Context.Stop(Sender);
      }
    }

    private void OnTerminated(Terminated terminatedMsg)
    {
      if (_loadIdByUserLoader.TryGetValue(terminatedMsg.ActorRef, out var loadId)
        && _authOriginByLoadId.TryGetValue(loadId, out var data))
      {
        _logger.Warning($"{nameof(OnTerminated)}: {terminatedMsg}");
        _loadIdByUserLoader.Remove(terminatedMsg.ActorRef);
        _authOriginByLoadId.Remove(loadId);

        var authErrorMsg = new UserAuthErrorMsg
        {
          AuthId = data.AuthMsg.AuthId,
          ErrorMessage = "user load error: unexpected",
        };
        data.AuthOrigin.Tell(authErrorMsg);
      }
    }

    public static Props Props()
      => Akka.Actor.Props
        .Create(() => new UserAuthenticatorActor())
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
