using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Actor.GameHub.Identity.Abtractions;
using Akka.Actor;
using Akka.Event;

namespace Actor.GameHub.Identity.Actors
{
  public class UserManagerActor : ReceiveActor
  {
    public class User
    {
      public Guid UserId { get; init; }
      public string Username { get; init; } = null!;
    }

    public class UserRepository
    {
      private readonly IDictionary<string, User> _usernameMap;
      private readonly IDictionary<Guid, User> _userIdMap;

      public UserRepository()
      {
        var users = new User[]
        {
          new User{ UserId = Guid.Parse("163C95EC-2705-484D-8B93-DBCD586D40CA"), Username = "lars" },
          new User{ UserId = Guid.Parse("3185B384-41E7-4CB1-BEC8-87B546B3CD20"), Username = "merten" },
          new User{ UserId = Guid.Parse("7B8FE1BF-084B-4575-A09D-09FA5E1B8F1F"), Username = "sam" },
          new User{ UserId = Guid.Parse("3281B126-DB29-4AD6-B7EA-FBE7FEB038A8"), Username = "uli" },
        };
        _usernameMap = users.ToDictionary(u => u.Username, u => u);
        _userIdMap = users.ToDictionary(u => u.UserId, u => u);
      }

      public User? FindByUserId(Guid userId)
      {
        if (_userIdMap.TryGetValue(userId, out var user))
          return user;

        return null;
      }

      public User? FindByUsername(string username)
      {
        if (_usernameMap.TryGetValue(username, out var user))
          return user;

        return null;
      }
    }

    private readonly ILoggingAdapter _logger = Context.GetLogger();
    private readonly UserRepository _userRepository = new();

    public UserManagerActor()
    {
      Receive<UserLoginMsg>(msg => !msg.IsValid(), msg => LoginError(msg, "username invalid"));
      Receive<UserLoginMsg>(msg => msg.Username.ToLowerInvariant() == "timeout", msg => { });
      ReceiveAsync<UserLoginMsg>(LoginUserAsync);
      ReceiveAsync<UserLogoutMsg>(LogoutUserAsync);
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

    private async Task<IActorRef?> UserHasSessionAsync(IUntypedActorContext context, Guid userId)
    {
      try
      {
        return await context
          .ActorSelection(IdentityMetadata.UserSessionName(userId))
          .ResolveOne(TimeSpan.FromSeconds(5.0))
          .ConfigureAwait(false);
      }
      catch (ActorNotFoundException)
      {
        return null;
      }
      catch
      {
        throw;
      }
    }

    private async Task LoginUserAsync(UserLoginMsg loginMsg)
    {
      var user = _userRepository.FindByUsername(loginMsg.Username);
      if (user is null)
      {
        LoginError(loginMsg, "user not found");
        return;
      }

      var context = Context;
      var sender = Sender;

      var userRef = await UserHasSessionAsync(context, user.UserId).ConfigureAwait(false);
      if (userRef is null)
      {
        var userAddress = sender.Path.Address;
        userRef = context.ActorOf(
          UserSessionActor.Props()
            .WithDeploy(Deploy.None.WithScope(new RemoteScope(userAddress))), IdentityMetadata.UserSessionName(user.UserId));
        context.Watch(userRef);
      }

      var successMsg = new UserLoginSuccessMsg
      {
        UserId = user.UserId,
        Username = user.Username,
      };
      userRef.Tell(successMsg, sender);

      _logger.Info($"{nameof(LoginUserAsync)} [{loginMsg.Username}]: {user.UserId} from {userRef.Path}");
    }

    private async Task LogoutUserAsync(UserLogoutMsg logoutMsg)
    {
      var context = Context;

      var userRef = await UserHasSessionAsync(context, logoutMsg.UserId).ConfigureAwait(false);
      if (userRef is not null)
      {
        context.Stop(userRef);

        _logger.Info($"{nameof(LogoutUserAsync)}: {logoutMsg.UserId}");
      }
    }

    private void OnTerminated(Terminated terminatedMsg)
    {
      _logger.Warning($"{nameof(OnTerminated)}: {terminatedMsg}");
    }

    public static Props Props()
      => Akka.Actor.Props.Create(() => new UserManagerActor());
  }
}
