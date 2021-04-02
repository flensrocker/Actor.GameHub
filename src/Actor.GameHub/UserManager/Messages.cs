using System;

namespace Actor.GameHub.UserManager.Messages
{
  public class UserLoginMsg
  {
    public string Username { get; init; }
  }

  public class UserLoginSuccessMsg
  {
    public Guid UserId { get; set; }
  }

  public class UserLoginErrorMsg
  {
    public string ErrorMessage { get; init; }
  }

  public class UserLogoutMsg
  {
    public Guid UserId { get; init; }
  }
}
