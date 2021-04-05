using Actor.GameHub.Identity.Abstractions;
using Akka.Actor;
using Akka.Event;

namespace Actor.GameHub.Identity.Actors
{
  public class ShellActor : ReceiveActor
  {
    private readonly ILoggingAdapter _logger = Context.GetLogger();

    private AddUserLoginMsg? _userLogin;

    public ShellActor()
    {
      Receive<AddUserLoginMsg>(AddLogin);
      Receive<LogoutUserMsg>(LogoutUser);
    }

    private void AddLogin(AddUserLoginMsg addLoginMsg)
    {
      if (_userLogin is not null)
        return;

      _userLogin = addLoginMsg;

      var loginSuccessMsg = new UserLoginSuccessMsg
      {
        UserLoginId = _userLogin.UserLoginId,
        ShellRef = Self,
        User = _userLogin.User,
      };
      _userLogin.LoginOrigin.Tell(loginSuccessMsg);

      _logger.Info($"LoginSuccess: send to {_userLogin.LoginOrigin.Path}");
    }

    private void LogoutUser(LogoutUserMsg logoutMsg)
    {
      if (_userLogin is null)
        return;

      Context.System.Stop(Self);

      _logger.Info($"user logged out from loginId {_userLogin?.UserLoginId}");
    }

    public static Props Props()
      => Akka.Actor.Props
        .Create(() => new ShellActor())
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
