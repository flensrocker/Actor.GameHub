namespace Actor.GameHub.Identity.Abstractions
{
  public class IdentityError
  {
    public int StatusCode { get; init; }
    public string Message { get; init; }

    public static IdentityError BadRequest(string message)
      => new() { StatusCode = 400, Message = message };

    public static IdentityError Forbidden(string message)
      => new() { StatusCode = 403, Message = message };

    public static IdentityError NotFound(string message)
      => new() { StatusCode = 404, Message = message };
  }
}
