using Actor.GameHub.Identity.Abstractions;
using Akka.Actor;
using Akka.Event;

namespace Actor.GameHub.Identity.Actors
{
  public class UserLoginActor : ReceiveActor
  {
    private readonly ILoggingAdapter _logger = Context.GetLogger();

    private UserLoginSuccessMsg? _loginSuccessMsg;

    public UserLoginActor()
    {
      Receive<UserLoginSuccessMsg>(UserLoginSuccess);
      Receive<LogoutUserMsg>(LogoutUser);
    }

    private void UserLoginSuccess(UserLoginSuccessMsg loginSuccessMsg)
    {
      _loginSuccessMsg = loginSuccessMsg;
     
      var loginMsg = new UserLoginMsg
      {
        UserLoginId = loginSuccessMsg.UserLoginId,
        UserLogin = Self,
        User = loginSuccessMsg.User,
      };
      loginSuccessMsg.LoginSender.Tell(loginMsg);
    }

    private void LogoutUser(LogoutUserMsg logoutMsg)
    {
      Context.System.Stop(Self);

      _logger.Info($"user logged out from loginId {_loginSuccessMsg?.UserLoginId}");
    }

    public static Props Props()
      => Akka.Actor.Props
        .Create(() => new UserLoginActor())
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
