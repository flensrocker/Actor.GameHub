using System;

namespace Actor.GameHub.Identity.Abstractions
{
  public abstract class IdentityException : Exception
  {
    public int StatusCode { get; init; }

    public IdentityException(int statusCode, string message)
      : base(message)
    {
      StatusCode = statusCode;
    }
  }

  public class IdentityBadRequestException : IdentityException
  {
    public IdentityBadRequestException(string message)
      : base(400, message)
    {
    }
  }

  public class IdentityForbiddenException : IdentityException
  {
    public IdentityForbiddenException(string message)
      : base(403, message)
    {
    }
  }

  public class IdentityNotFoundException : IdentityException
  {
    public IdentityNotFoundException(string message)
      : base(404, message)
    {
    }
  }
}
