using System;
using System.Collections.Generic;
using Actor.GameHub.Identity.Abtractions;
using Akka.Actor;
using Akka.Event;

namespace Actor.GameHub.Identity.Actors
{
  public class UserManagerActor : ReceiveActor
  {
    private class User
    {
      public Guid UserId { get; init; }
      public string Username { get; init; } = null!;
      public IActorRef UserActor { get; init; } = null!;
    }

    private readonly ILoggingAdapter _logger = Context.GetLogger();

    private readonly IDictionary<string, User> _usernameMap = new Dictionary<string, User>();
    private readonly IDictionary<Guid, User> _userIdMap = new Dictionary<Guid, User>();
    private readonly IDictionary<IActorRef, User> _userActorMap = new Dictionary<IActorRef, User>();

    public UserManagerActor()
    {
      Receive<UserLoginMsg>(msg => string.IsNullOrWhiteSpace(msg.Username), msg => LoginError(msg, "username required"));
      Receive<UserLoginMsg>(msg => _usernameMap.ContainsKey(msg.Username), msg => LoginError(msg, "username invalid"));
      Receive<UserLoginMsg>(msg => msg.Username.ToLowerInvariant() == "timeout", msg => { });
      Receive<UserLoginMsg>(LoginUser);
      Receive<UserLogoutMsg>(msg => _userIdMap.ContainsKey(msg.UserId), LogoutUser);
      Receive<Terminated>(OnTerminated);
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
      var userAddress = Sender.Path.Address;
      var userId = Guid.NewGuid();
      var userRef = Context.ActorOf(
        UserActor.Props()
          .WithDeploy(Deploy.None.WithScope(new RemoteScope(userAddress))), $"User-{userId}");
      Context.Watch(userRef);

      var user = new User
      {
        UserId = userId,
        Username = loginMsg.Username,
        UserActor = userRef,
      };

      _usernameMap.Add(loginMsg.Username, user);
      _userIdMap.Add(user.UserId, user);
      _userActorMap.Add(userRef, user);

      var successMsg = new UserLoginSuccessMsg
      {
        UserId = user.UserId,
        Username = loginMsg.Username,
      };
      userRef.Tell(successMsg);
      Sender.Tell(successMsg);

      _logger.Info($"{nameof(LoginUser)} [{loginMsg.Username}]: {user.UserId} from {userAddress}");
    }

    private void LogoutUser(UserLogoutMsg logoutMsg)
    {
      if (_userIdMap.TryGetValue(logoutMsg.UserId, out var user))
      {
        Context.Stop(user.UserActor);

        _logger.Info($"{nameof(LogoutUser)} [{user.Username}]: {user.UserId}");
      }
    }

    private void OnTerminated(Terminated terminatedMsg)
    {
      _logger.Warning($"{nameof(OnTerminated)}: {terminatedMsg}");

      if (_userActorMap.Remove(terminatedMsg.ActorRef, out var user))
      {
        _userIdMap.Remove(user.UserId);
        _usernameMap.Remove(user.Username);
      }
    }

    public static Props Props()
      => Akka.Actor.Props.Create(() => new UserManagerActor());
  }
}
