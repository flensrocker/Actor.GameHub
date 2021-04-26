namespace Actor.GameHub.Terminal
{
  public static partial class TerminalExtensions
  {
    public static string? SplitFirstWord(this string? words, out string? rest)
    {
      rest = null;
      if (string.IsNullOrWhiteSpace(words))
        return null;

      var spaceIndex = words.IndexOf(' ');
      if (spaceIndex < 0)
        return words;

      var firstWord = words.Substring(0, spaceIndex);
      if (spaceIndex < words.Length - 1)
        rest = words.Substring(spaceIndex + 1);
      return firstWord;
    }
  }
}
