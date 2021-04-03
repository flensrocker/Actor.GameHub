using System;
using System.Collections.Generic;
using System.Linq;
using Actor.GameHub.Identity.Abtractions;

namespace Actor.GameHub.Identity.Actors
{
  public class UserRepository
  {
    private readonly IDictionary<string, User> _usernameMap;
    private readonly IDictionary<Guid, User> _userIdMap;

    public UserRepository()
    {
      var users = new User[]
      {
          new User{ UserId = Guid.Parse("163C95EC-2705-484D-8B93-DBCD586D40CA"), Username = "lars" },
          new User{ UserId = Guid.Parse("3185B384-41E7-4CB1-BEC8-87B546B3CD20"), Username = "merten" },
          new User{ UserId = Guid.Parse("7B8FE1BF-084B-4575-A09D-09FA5E1B8F1F"), Username = "sam" },
          new User{ UserId = Guid.Parse("3281B126-DB29-4AD6-B7EA-FBE7FEB038A8"), Username = "uli" },
      };
      _usernameMap = users.ToDictionary(u => u.Username, u => u);
      _userIdMap = users.ToDictionary(u => u.UserId, u => u);
    }

    public User? FindByUserId(Guid userId)
    {
      if (_userIdMap.TryGetValue(userId, out var user))
        return user;

      return null;
    }

    public User? FindByUsername(string username)
    {
      if (_usernameMap.TryGetValue(username, out var user))
        return user;

      return null;
    }
  }
}
