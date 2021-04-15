using System;

namespace Actor.GameHub.Identity.EntityFrameworkCore
{
  public class UserEntity
  {
    public Guid Id { get; init; }
    public string Username { get; init; } = null!;
  }
}
