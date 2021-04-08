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
      Become(ReceiveAuth);
    }

    private void ReceiveAuth()
    {
      Receive<AuthUserMsg>(AuthUser);
    }

    private void ReceiveLoad()
    {
      Receive<UserLoadForAuthErrorMsg>(UserLoadError);
      Receive<UserLoadForAuthSuccessMsg>(UserLoadSuccess);
      Receive<Terminated>(OnTerminated);
    }

    private void AuthUser(AuthUserMsg authMsg)
    {
      var loadUserMsg = new LoadUserByUsernameForAuthMsg
      {
        LoadId = Guid.NewGuid(),
        Username = authMsg.LoginUserMsg.Username,
      };

      if (_authOriginByLoadId.TryAdd(loadUserMsg.LoadId, (authMsg, Sender)))
      {
        var userLoader = Context.ActorOf(UserLoaderActor.Props(), IdentityMetadata.UserLoaderName(loadUserMsg.LoadId));
        _loadIdByUserLoader.Add(userLoader, loadUserMsg.LoadId);

        Context.Watch(userLoader);
        userLoader.Tell(loadUserMsg);

        Become(ReceiveLoad);
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

    private void UserLoadError(UserLoadForAuthErrorMsg loadErrorMsg)
    {
      if (_authOriginByLoadId.TryGetValue(loadErrorMsg.LoadId, out var data)
        && _loadIdByUserLoader.ContainsKey(Sender))
      {
        var authErrorMsg = new UserAuthErrorMsg
        {
          AuthId = data.AuthMsg.AuthId,
          ErrorMessage = loadErrorMsg.ErrorMessage,
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
      var loaderRef = terminatedMsg.ActorRef;

      if (_loadIdByUserLoader.TryGetValue(loaderRef, out var loadId)
        && _authOriginByLoadId.TryGetValue(loadId, out var data))
      {
        _logger.Warning($"{nameof(OnTerminated)}: unexpected stop of loader {loadId}, {loaderRef.Path}");
        _loadIdByUserLoader.Remove(loaderRef);
        _authOriginByLoadId.Remove(loadId);

        var authErrorMsg = new UserAuthErrorMsg
        {
          AuthId = data.AuthMsg.AuthId,
          ErrorMessage = "user login error: unexpected stop of loader",
        };
        data.AuthOrigin.Tell(authErrorMsg);

        Context.System.Stop(Self);
      }
    }

    public static Props Props()
      => Akka.Actor.Props
        .Create<UserAuthenticatorActor>()
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
