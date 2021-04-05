using Actor.GameHub.Identity.Abstractions;
using Akka.Actor;

namespace Actor.GameHub.Identity.Actors
{
  public class ShellActor : ReceiveActor
  {
    private AddUserLoginMsg? _userLogin;

    public ShellActor()
    {
      Become(ReceiveLogin);
    }

    private void ReceiveLogin()
    {
      Receive<AddUserLoginMsg>(AddLogin);
    }

    private void ReceiveLoggedIn()
    {
      Receive<LogoutUserMsg>(LogoutUser);
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

      Become(ReceiveLoggedIn);
    }

    private void LogoutUser(LogoutUserMsg logoutMsg)
    {
      System.Diagnostics.Debug.Assert(_userLogin is not null);

      Context.System.Stop(Self);
    }

    public static Props Props()
      => Akka.Actor.Props
        .Create(() => new ShellActor())
        .WithSupervisorStrategy(new StoppingSupervisorStrategy().Create());
  }
}
