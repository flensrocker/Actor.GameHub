using System;
using System.Collections.Generic;
using Actor.GameHub.Identity.Abtractions;
using Akka.Actor;
using Akka.Event;

namespace Actor.GameHub.Identity.Actors
{
  public class UserSessionManagerActor : ReceiveActor
  {
    private readonly ILoggingAdapter _logger = Context.GetLogger();

    private readonly Dictionary<Guid, (LoginUserMsg LoginMsg, IActorRef Sender)> _loginSenderByAuthId = new();
    private readonly Dictionary<IActorRef, Guid> _authIdByUserAuthenticater = new();

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

      if (_loginSenderByAuthId.TryAdd(authUserMsg.AuthId, (loginMsg, Sender)))
      {
        var userAuthenticator = Context.ActorOf(UserAuthenticatorActor.Props(), IdentityMetadata.UserAuthenticatorName(authUserMsg.AuthId));
        Context.Watch(userAuthenticator);
        userAuthenticator.Tell(authUserMsg);

        _authIdByUserAuthenticater.Add(userAuthenticator, authUserMsg.AuthId);
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
      if (_loginSenderByAuthId.Remove(authErrorMsg.AuthId, out var data))
      {
        var loginErrorMsg = new UserLoginErrorMsg
        {
          ErrorMessage = $"user auth error: {authErrorMsg.ErrorMessage}",
        };
        data.Sender.Tell(loginErrorMsg);

        _authIdByUserAuthenticater.Remove(Sender);
      }
    }

    private void UserAuthSuccess(UserAuthSuccessMsg authSuccessMsg)
    {
      if (_loginSenderByAuthId.Remove(authSuccessMsg.AuthId, out var data))
      {
        var loginSuccessMsg = new UserLoginSuccessMsg
        {
          LoginSender = data.Sender,
          User = authSuccessMsg.User,
        };

        var sessionName = IdentityMetadata.UserSessionName(authSuccessMsg.User.UserId);
        var session = Context.Child(sessionName);
        if (session is null || session == ActorRefs.Nobody)
          session = Context.ActorOf(UserSessionActor.Props(), sessionName);
        session.Tell(loginSuccessMsg);

        _authIdByUserAuthenticater.Remove(Sender);
      }
    }

    private void OnTerminated(Terminated terminatedMsg)
    {
      if (_authIdByUserAuthenticater.Remove(terminatedMsg.ActorRef, out var authId)
        && _loginSenderByAuthId.Remove(authId, out var data))
      {
        var loginErrorMsg = new UserLoginErrorMsg
        {
          ErrorMessage = "user login error: unexpected",
        };
        data.Sender.Tell(loginErrorMsg);
      }

      _logger.Warning($"{nameof(OnTerminated)}: {terminatedMsg}");
    }

    public static Props Props()
      => Akka.Actor.Props.Create(() => new UserSessionManagerActor());
  }
}
