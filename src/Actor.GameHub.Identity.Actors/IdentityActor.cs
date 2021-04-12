using System;
using System.Collections.Generic;
using Actor.GameHub.Identity.Abstractions;
using Akka.Actor;
using Akka.Event;

namespace Actor.GameHub.Identity.Actors
{
  public class IdentityActor : ReceiveActor
  {
    private readonly ILoggingAdapter _logger = Context.GetLogger();

    private readonly Dictionary<Guid, (LoginUserMsg LoginMsg, IActorRef LoginOrigin)> _loginOriginByAuthId = new();
    private readonly Dictionary<IActorRef, Guid> _authIdByAuthenticatorRef = new();

    public IdentityActor()
    {
      Receive<LoginUserMsg>(msg => !msg.IsValid(), LoginInvalid);
      Receive<LoginUserMsg>(LoginUser);
      Receive<UserAuthErrorMsg>(AuthError);
      Receive<UserAuthSuccessMsg>(AuthSuccess);
      Receive<Terminated>(OnTerminated);

      _logger.Info("==> Identity started");
    }

    private void LoginInvalid(LoginUserMsg loginMsg)
    {
      Sender.Tell(new UserLoginErrorMsg
      {
        UserLoginId = loginMsg.UserLoginId,
        ErrorMessage = "invalid login",
      });
    }

    private void LoginUser(LoginUserMsg loginMsg)
    {
      var authUserMsg = new AuthUserMsg
      {
        AuthId = Guid.NewGuid(),
        LoginUserMsg = loginMsg,
      };

      if (_loginOriginByAuthId.TryAdd(authUserMsg.AuthId, (loginMsg, Sender)))
      {
        var authenticator = Context.ActorOf(UserAuthenticatorActor.Props(), IdentityMetadata.UserAuthenticatorName(authUserMsg.AuthId));
        _authIdByAuthenticatorRef.Add(authenticator, authUserMsg.AuthId);
        Context.Watch(authenticator);
        authenticator.Tell(authUserMsg);
      }
      else
      {
        var loginErrorMsg = new UserLoginErrorMsg
        {
          UserLoginId = loginMsg.UserLoginId,
          ErrorMessage = "user login authId error, try again...",
        };
        Sender.Tell(loginErrorMsg);
      }
    }

    private void AuthError(UserAuthErrorMsg authErrorMsg)
    {
      var authenticatorRef = Sender;

      if (_loginOriginByAuthId.TryGetValue(authErrorMsg.AuthId, out var data)
        && _authIdByAuthenticatorRef.ContainsKey(authenticatorRef))
      {
        var loginErrorMsg = new UserLoginErrorMsg
        {
          UserLoginId = data.LoginMsg.UserLoginId,
          ErrorMessage = authErrorMsg.ErrorMessage,
        };
        data.LoginOrigin.Tell(loginErrorMsg);

        _loginOriginByAuthId.Remove(authErrorMsg.AuthId);
        _authIdByAuthenticatorRef.Remove(authenticatorRef);
        Context.Stop(authenticatorRef);
      }
    }

    private void AuthSuccess(UserAuthSuccessMsg authSuccessMsg)
    {
      var authenticatorRef = Sender;

      if (_loginOriginByAuthId.TryGetValue(authSuccessMsg.AuthId, out var data)
        && _authIdByAuthenticatorRef.ContainsKey(authenticatorRef))
      {
        var loginSuccessMsg = new UserLoginSuccessMsg
        {
          UserLoginId = data.LoginMsg.UserLoginId,
          User = authSuccessMsg.User,
        };
        data.LoginOrigin.Tell(loginSuccessMsg);

        _loginOriginByAuthId.Remove(authSuccessMsg.AuthId);
        _authIdByAuthenticatorRef.Remove(authenticatorRef);
        Context.Stop(authenticatorRef);
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
      => Akka.Actor.Props.Create<IdentityActor>();
  }
}
