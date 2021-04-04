using Actor.GameHub.Identity.Abstractions;
using Akka.Actor;
using Akka.Event;

namespace Actor.GameHub.Identity.Actors
{
  public class UserLoginActor : ReceiveActor
  {
    private readonly ILoggingAdapter _logger = Context.GetLogger();

    private AddUserLoginMsg? _userLogin;

    public UserLoginActor()
    {
      Receive<AddUserLoginMsg>(AddLogin);
      Receive<LogoutUserMsg>(LogoutUser);
    }

    private void AddLogin(AddUserLoginMsg addLoginMsg)
    {
      _userLogin = addLoginMsg;

      var loginSuccessMsg = new UserLoginSuccessMsg
      {
        UserLoginId = _userLogin.UserLoginId,
        UserLogin = Self,
        User = _userLogin.User,
      };
      _userLogin.LoginSender.Tell(loginSuccessMsg);

      _logger.Info($"LoginSuccess: send to {_userLogin.LoginSender.Path}");
    }

    private void LogoutUser(LogoutUserMsg logoutMsg)
    {
      Context.System.Stop(Self);

      _logger.Info($"user logged out from loginId {_userLogin?.UserLoginId}");
    }

    public static Props Props()
      => Akka.Actor.Props
        .Create(() => new UserLoginActor())
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
