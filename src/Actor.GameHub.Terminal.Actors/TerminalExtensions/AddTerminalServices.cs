﻿using Actor.GameHub.Terminal.Abstractions;
using Actor.GameHub.Terminal.Actors;
using Microsoft.Extensions.DependencyInjection;

namespace Actor.GameHub.Terminal
{
  public static partial class TerminalExtensions
  {
    public static IServiceCollection AddTerminalServices(this IServiceCollection services)
    {
      services.AddSingleton<ITerminalCommand, TerminalExitCommand>();
      services.AddSingleton<ITerminalCommand, TerminalEchoCommand>();

      return services;
    }
  }
}