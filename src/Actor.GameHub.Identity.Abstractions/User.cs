using System;

namespace Actor.GameHub.Identity.Abstractions
{
  public class User
  {
    public Guid UserId { get; init; }
    public string Username { get; init; } = null!;
  }
}
