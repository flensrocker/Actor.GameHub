using System;

namespace Actor.GameHub.Identity.Abtractions
{
  public interface IUserManagerMsg
  {
  }

  public class UserLogoutMsg : IUserManagerMsg
  {
    public Guid UserId { get; init; }
  }

  public class UserLoginMsg : IUserManagerMsg
  {
    public string Username { get; init; } = null!;
  }

  public class UserLoginSuccessMsg : IUserManagerMsg
  {
    public Guid UserId { get; set; }
  }

  public class UserLoginErrorMsg : IUserManagerMsg
  {
    public string ErrorMessage { get; init; } = null!;
  }
}
