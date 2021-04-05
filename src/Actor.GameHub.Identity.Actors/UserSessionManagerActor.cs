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
    private readonly Dictionary<IActorRef, Guid> _authIdByUserAuthenticator = new();

    public UserSessionManagerActor()
    {
      Receive<LoginUserMsg>(msg => !msg.IsValid(), msg => LoginError(msg, "username invalid"));
      Receive<LoginUserMsg>(msg => msg.Username.ToLowerInvariant() == "timeout", msg => { });
      Receive<LoginUserMsg>(LoginUser);
      Receive<UserAuthErrorMsg>(UserAuthError);
      Receive<UserAuthSuccessMsg>(UserAuthSuccess);
      Receive<Terminated>(OnTerminated);
    }

    private void LoginError(LoginUserMsg loginMsg, string errorMessage)
    {
      _logger.Error($"{nameof(LoginError)} [{loginMsg.Username}]: {errorMessage}");

      Sender.Tell(new UserLoginErrorMsg
      {
        ErrorMessage = errorMessage,
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
        var userAuthenticator = Context.ActorOf(UserAuthenticatorActor.Props(), IdentityMetadata.UserAuthenticatorName(authUserMsg.AuthId));
        Context.Watch(userAuthenticator);
        userAuthenticator.Tell(authUserMsg);

        _authIdByUserAuthenticator.Add(userAuthenticator, authUserMsg.AuthId);
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

    private void UserAuthError(UserAuthErrorMsg authErrorMsg)
    {
      if (_loginOriginByAuthId.TryGetValue(authErrorMsg.AuthId, out var data)
        && _authIdByUserAuthenticator.ContainsKey(Sender))
      {
        var loginErrorMsg = new UserLoginErrorMsg
        {
          ErrorMessage = $"user auth error: {authErrorMsg.ErrorMessage}",
        };
        data.LoginOrigin.Tell(loginErrorMsg);

        _loginOriginByAuthId.Remove(authErrorMsg.AuthId);
        _authIdByUserAuthenticator.Remove(Sender);
        Context.Stop(Sender);
      }
    }

    private void UserAuthSuccess(UserAuthSuccessMsg authSuccessMsg)
    {
      if (_loginOriginByAuthId.TryGetValue(authSuccessMsg.AuthId, out var data)
        && _authIdByUserAuthenticator.ContainsKey(Sender))
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
        _authIdByUserAuthenticator.Remove(Sender);
        Context.Stop(Sender);
      }
    }

    private void OnTerminated(Terminated terminatedMsg)
    {
      if (_authIdByUserAuthenticator.TryGetValue(terminatedMsg.ActorRef, out var authId)
        && _loginOriginByAuthId.TryGetValue(authId, out var data))
      {
        _logger.Warning($"{nameof(OnTerminated)}: {terminatedMsg}");
        _authIdByUserAuthenticator.Remove(terminatedMsg.ActorRef);
        _loginOriginByAuthId.Remove(authId);

        var loginErrorMsg = new UserLoginErrorMsg
        {
          ErrorMessage = "user login error: unexpected",
        };
        data.LoginOrigin.Tell(loginErrorMsg);
      }
    }

    public static Props Props()
      => Akka.Actor.Props.Create(() => new UserSessionManagerActor());
  }
}
