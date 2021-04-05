using System;
using System.Collections.Generic;
using Actor.GameHub.Identity.Abstractions;
using Akka.Actor;
using Akka.Event;

namespace Actor.GameHub.Identity.Actors
{
  public class UserSessionManagerActor : ReceiveActor
  {
    private readonly ILoggingAdapter _logger = Context.GetLogger();

    private readonly Dictionary<Guid, (LoginUserMsg LoginMsg, IActorRef LoginOrigin)> _loginOriginByAuthId = new();
    private readonly Dictionary<IActorRef, Guid> _authIdByAuthenticatorRef = new();

    public UserSessionManagerActor()
    {
      Receive<LoginUserMsg>(msg => !msg.IsValid(), LoginInvalid);
      Receive<LoginUserMsg>(msg => msg.Username.ToLowerInvariant() == "timeout", msg => { });
      Receive<LoginUserMsg>(LoginUser);
      Receive<UserAuthErrorMsg>(AuthError);
      Receive<UserAuthSuccessMsg>(AuthSuccess);
      Receive<Terminated>(OnTerminated);
    }

    private void LoginInvalid(LoginUserMsg loginMsg)
    {
      Sender.Tell(new UserLoginErrorMsg
      {
        ErrorMessage = "login invalid",
      });
    }

    private void LoginUser(LoginUserMsg loginMsg)
    {
      var authUserMsg = new AuthUserMsg
      {
        LoginUserMsg = loginMsg,
      };

      if (_loginOriginByAuthId.TryAdd(authUserMsg.AuthId, (loginMsg, Sender)))
      {
        _logger.Info($"==> Authenticator {authUserMsg.AuthId} created");

        var authenticator = Context.ActorOf(UserAuthenticatorActor.Props(), IdentityMetadata.UserAuthenticatorName(authUserMsg.AuthId));
        _authIdByAuthenticatorRef.Add(authenticator, authUserMsg.AuthId);
        Context.Watch(authenticator);
        authenticator.Tell(authUserMsg);
      }
      else
      {
        var loginErrorMsg = new UserLoginErrorMsg
        {
          ErrorMessage = "user login authId error, try again",
        };
        Sender.Tell(loginErrorMsg);
      }
    }

    private void AuthError(UserAuthErrorMsg authErrorMsg)
    {
      if (_loginOriginByAuthId.TryGetValue(authErrorMsg.AuthId, out var data)
        && _authIdByAuthenticatorRef.ContainsKey(Sender))
      {
        var loginErrorMsg = new UserLoginErrorMsg
        {
          ErrorMessage = $"auth error: {authErrorMsg.ErrorMessage}",
        };
        data.LoginOrigin.Tell(loginErrorMsg);

        _loginOriginByAuthId.Remove(authErrorMsg.AuthId);
        _authIdByAuthenticatorRef.Remove(Sender);
        Context.Stop(Sender);
        _logger.Info($"==> Authenticator {authErrorMsg.AuthId} stopped");
      }
    }

    private void AuthSuccess(UserAuthSuccessMsg authSuccessMsg)
    {
      var loaderRef = Sender;

      if (_loginOriginByAuthId.TryGetValue(authSuccessMsg.AuthId, out var data)
        && _authIdByAuthenticatorRef.ContainsKey(loaderRef))
      {
        var loginSuccessMsg = new AddUserLoginMsg
        {
          LoginOrigin = data.LoginOrigin,
          User = authSuccessMsg.User,
        };

        var sessionName = IdentityMetadata.UserSessionName(authSuccessMsg.User.UserId);
        var session = Context.Child(sessionName);
        if (session is null || session == ActorRefs.Nobody)
          session = Context.ActorOf(UserSessionActor.Props(), sessionName);
        session.Tell(loginSuccessMsg);

        _loginOriginByAuthId.Remove(authSuccessMsg.AuthId);
        _authIdByAuthenticatorRef.Remove(loaderRef);
        Context.Stop(loaderRef);
        _logger.Info($"==> Authenticator {authSuccessMsg.AuthId} stopped");
      }
    }

    private void OnTerminated(Terminated terminatedMsg)
    {
      var authRef = terminatedMsg.ActorRef;

      if (_authIdByAuthenticatorRef.TryGetValue(authRef, out var authId)
        && _loginOriginByAuthId.TryGetValue(authId, out var data))
      {
        _logger.Warning($"{nameof(OnTerminated)}: unexpected stop of authenticator {authId}, {authRef.Path}");
        _authIdByAuthenticatorRef.Remove(authRef);
        _loginOriginByAuthId.Remove(authId);

        var loginErrorMsg = new UserLoginErrorMsg
        {
          ErrorMessage = "user login error, unexpected stop of authenticator",
        };
        data.LoginOrigin.Tell(loginErrorMsg);
      }
    }

    public static Props Props()
      => Akka.Actor.Props.Create(() => new UserSessionManagerActor());
  }
}
