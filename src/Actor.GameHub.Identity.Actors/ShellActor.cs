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
      Become(ReceiveLogin);
    }

    private void ReceiveLogin()
    {
      Receive<AddUserLoginMsg>(AddLogin);
      Receive<Terminated>(OnTerminated);
    }

    private void ReceiveLoggedIn()
    {
      Receive<LogoutUserMsg>(LogoutUser);
      Receive<Terminated>(OnTerminated);
    }

    private void AddLogin(AddUserLoginMsg addLoginMsg)
    {
      System.Diagnostics.Debug.Assert(_userLogin is null);

      _userLogin = addLoginMsg;

      var loginSuccessMsg = new UserLoginSuccessMsg
      {
        UserLoginId = _userLogin.UserLoginId,
        ShellRef = Self,
        User = _userLogin.User,
      };
      _userLogin.LoginOrigin.Tell(loginSuccessMsg);
      Context.Watch(_userLogin.LoginOrigin);

      Become(ReceiveLoggedIn);
    }

    private void LogoutUser(LogoutUserMsg logoutMsg)
    {
      System.Diagnostics.Debug.Assert(_userLogin is not null);

      Context.Unwatch(_userLogin.LoginOrigin);
      _userLogin = null;
      Context.System.Stop(Self);
    }

    private void OnTerminated(Terminated terminatedMsg)
    {
      if (_userLogin is not null)
      {
        _logger.Warning($"==> login-origin {_userLogin.LoginOrigin.Path} terminated, exiting");
        Context.System.Stop(Self);
      }
    }

    public static Props Props()
      => Akka.Actor.Props
        .Create(() => new ShellActor())
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
