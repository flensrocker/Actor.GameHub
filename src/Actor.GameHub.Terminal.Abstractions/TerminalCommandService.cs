using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;

namespace Actor.GameHub.Terminal.Abstractions
{
  public class TerminalCommandService
  {
    private readonly Dictionary<string, ITerminalCommand> _commands;

    public TerminalCommandService(IServiceProvider serviceProvider)
    {
      _commands = serviceProvider
        .GetServices<ITerminalCommand>()
        .ToDictionary(cmd => cmd.Command, cmd => cmd);
    }

    public Props? Props(string command)
    {
      if (_commands.TryGetValue(command, out var cmd))
        return cmd.Props();

      return null;
    }
  }
}
