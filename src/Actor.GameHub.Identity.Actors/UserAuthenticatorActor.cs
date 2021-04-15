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
      var authOrigin = Sender;

      var loadUserMsg = new LoadUserByUsernameForAuthMsg
      {
        LoadId = Guid.NewGuid(),
        Username = authMsg.LoginUserMsg.Username,
      };

      if (_authOriginByLoadId.TryAdd(loadUserMsg.LoadId, (authMsg, authOrigin)))
      {
        var loaderRef = Context.ActorOf(UserLoaderActor.Props(Context.System), IdentityMetadata.UserLoaderName(loadUserMsg.LoadId));
        _loadIdByUserLoader.Add(loaderRef, loadUserMsg.LoadId);

        Context.Watch(loaderRef);
        loaderRef.Tell(loadUserMsg);

        Become(ReceiveLoad);
      }
      else
      {
        var authErrorMsg = new UserAuthErrorMsg
        {
          AuthId = authMsg.AuthId,
          ErrorMessage = "user auth loadId error, try again...",
        };
        authOrigin.Tell(authErrorMsg);
      }
    }

    private void UserLoadError(UserLoadForAuthErrorMsg loadErrorMsg)
    {
      var loaderRef = Sender;

      if (_authOriginByLoadId.TryGetValue(loadErrorMsg.LoadId, out var data)
        && _loadIdByUserLoader.ContainsKey(loaderRef))
      {
        var authErrorMsg = new UserAuthErrorMsg
        {
          AuthId = data.AuthMsg.AuthId,
          ErrorMessage = loadErrorMsg.ErrorMessage,
        };
        data.AuthOrigin.Tell(authErrorMsg);

        _authOriginByLoadId.Remove(loadErrorMsg.LoadId);
        _loadIdByUserLoader.Remove(loaderRef);
        Context.Stop(loaderRef);
      }
    }

    private void UserLoadSuccess(UserLoadForAuthSuccessMsg loadSuccessMsg)
    {
      var loaderRef = Sender;

      if (_authOriginByLoadId.TryGetValue(loadSuccessMsg.LoadId, out var data)
        && _loadIdByUserLoader.ContainsKey(loaderRef))
      {
        // TODO do authentication based on password/idToken

        var authSuccessMsg = new UserAuthSuccessMsg
        {
          AuthId = data.AuthMsg.AuthId,
          User = new User { UserId = loadSuccessMsg.User.UserId, Username = loadSuccessMsg.User.Username },
        };
        data.AuthOrigin.Tell(authSuccessMsg);

        _authOriginByLoadId.Remove(loadSuccessMsg.LoadId);
        _loadIdByUserLoader.Remove(loaderRef);
        Context.Stop(loaderRef);
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

        Context.Stop(Self);
      }
    }

    public static Props Props()
      => Akka.Actor.Props
        .Create<UserAuthenticatorActor>()
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
