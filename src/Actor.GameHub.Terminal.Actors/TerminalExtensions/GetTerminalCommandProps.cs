using System;
using System.Linq;
using Actor.GameHub.Terminal.Abstractions;
using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;

namespace Actor.GameHub.Terminal
{
  public static partial class TerminalExtensions
  {
    public static Props? GetTerminalCommandProps(this IServiceProvider serviceProvider, string command)
    {
      return serviceProvider
        .GetServices<ITerminalCommand>()
        .Where(cmd => cmd.Command == command)
        .FirstOrDefault()?
        .Props();
    }
  }
}
