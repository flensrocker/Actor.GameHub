using System.Linq;
using Actor.GameHub.Identity.Abtractions;

namespace Actor.GameHub.Identity
{
  public static partial class IdentityExtensions
  {
    public static bool IsValid(this LoginUserMsg msg)
    {
      if (msg is null || string.IsNullOrWhiteSpace(msg.Username))
        return false;

      return char.IsLetter(msg.Username, 0)
        && msg.Username.Equals(msg.Username.Trim().ToLowerInvariant())
        && msg.Username.All(char.IsLetterOrDigit);
    }
  }
}
