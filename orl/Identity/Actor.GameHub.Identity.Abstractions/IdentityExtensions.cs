using System;
using Orleans;

namespace Actor.GameHub.Identity.Abstractions
{
  public static class IdentityExtensions
  {
    public static IPlayerByUsername GetPlayerByUsername(this IGrainFactory factory, string username)
      => factory.GetGrain<IPlayerByUsername>(username.ToLowerInvariant());

    public static IPlayerById GetPlayerById(this IGrainFactory factory, Guid playerId)
      => factory.GetGrain<IPlayerById>(playerId);
  }
}
