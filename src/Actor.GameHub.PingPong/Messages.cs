using System;

namespace Actor.GameHub.PingPong.Messages
{
  public class PingMsg
  {
    public long Number { get; init; }
  }

  public class PongMsg
  {
    public long Number { get; init; }
  }

  public class GameOverMsg
  {
  }

  public class ScoreMsg
  {
    public Guid UserId { get; init; }
    public long PingCount { get; init; }
    public long PingSum { get; init; }
    public long PongCount { get; init; }
    public long PongSum { get; init; }
  }
}
