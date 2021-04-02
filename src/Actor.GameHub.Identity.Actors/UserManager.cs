using System;
using System.Collections.Generic;
using Actor.GameHub.Identity.Abtractions;
using Akka.Actor;
using Akka.Event;

namespace Actor.GameHub.Identity
{
  public class UserManager : ReceiveActor
  {
    private class User
    {
      public Guid UserId { get; } = Guid.NewGuid();
      public string Username { get; init; }
      public IActorRef UserActor { get; init; }
    }

    private readonly ILoggingAdapter _logger = Context.GetLogger();

    private readonly IDictionary<string, User> _usernameMap = new Dictionary<string, User>();
    private readonly IDictionary<Guid, User> _userIdMap = new Dictionary<Guid, User>();

    public UserManager()
    {
      Receive<UserLoginMsg>(msg => string.IsNullOrWhiteSpace(msg.Username), msg => LoginError(msg, "username required"));
      Receive<UserLoginMsg>(msg => _usernameMap.ContainsKey(msg.Username), msg => LoginError(msg, "username invalid"));
      Receive<UserLoginMsg>(msg => msg.Username.ToLowerInvariant() == "timeout", msg => { });
      Receive<UserLoginMsg>(LoginUser);
      Receive<UserLogoutMsg>(msg => _userIdMap.ContainsKey(msg.UserId), LogoutUser);
    }

    private void LoginError(UserLoginMsg loginMsg, string errorMessage)
    {
      _logger.Error($"{nameof(LoginError)} [{loginMsg.Username}]: {errorMessage}");

      Sender.Tell(new UserLoginErrorMsg
      {
        ErrorMessage = errorMessage,
      });
    }

    private void LoginUser(UserLoginMsg loginMsg)
    {
      var user = new User
      {
        Username = loginMsg.Username,
        UserActor = Sender,
      };

      _usernameMap.Add(loginMsg.Username, user);
      _userIdMap.Add(user.UserId, user);
      Sender.Tell(new UserLoginSuccessMsg { UserId = user.UserId });

      _logger.Info($"{nameof(LoginUser)} [{loginMsg.Username}]: {user.UserId}");
    }

    private void LogoutUser(UserLogoutMsg logoutMsg)
    {
      _userIdMap.Remove(logoutMsg.UserId, out var user);
      _usernameMap.Remove(user.Username);

      _logger.Info($"{nameof(LogoutUser)} [{user.Username}]: {user.UserId}");
    }

    public static Props Props()
      => Akka.Actor.Props.Create(() => new UserManager());
  }
}
