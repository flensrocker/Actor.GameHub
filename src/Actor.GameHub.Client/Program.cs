using System;
using System.IO;
using System.Threading.Tasks;
using Actor.GameHub.Identity.Abstractions;
using Actor.GameHub.Terminal;
using Actor.GameHub.Terminal.Abstractions;
using Akka.Actor;
using Akka.Configuration;

namespace Actor.GameHub.Client
{
  class Program
  {
    static async Task Main(string[] args)
    {
      var config = File.Exists("app.config")
        ? ConfigurationFactory.ParseString(await File.ReadAllTextAsync("app.config"))
        : ConfigurationFactory.Default();

      var gameHubClientSystem = ActorSystem.Create("GameHubClient", config);

      var consoleRef = gameHubClientSystem.ActorOf(ConsoleActor.Props(), "Console");

      var username = "";
      object? response = null;
      var run = true;
      do
      {
        do
        {
          Console.Write("login: ");
          username = Console.ReadLine();
          if (!string.IsNullOrWhiteSpace(username))
          {
            username = username.Trim();
            break;
          }
        } while (true);

        var openTerminalMsg = new OpenTerminalMsg
        {
          LoginUser = new LoginUserMsg
          {
            Username = username,
          },
        };
        try
        {
          response = await consoleRef.Ask(openTerminalMsg, TimeSpan.FromSeconds(10.0)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
          await Console.Error.WriteLineAsync($"Cannot open terminal: {ex.Message}");
        }

        switch (response)
        {
          case null:
            {
              Console.Error.WriteLine("no response");
              break;
            }
          case TerminalOpenErrorMsg terminalErrorMsg:
            {
              Console.Error.WriteLine($"Terminal error: {terminalErrorMsg.ErrorMessage}");
              break;
            }
          case TerminalOpenSuccessMsg terminalSession:
            {
              Console.WriteLine($"terminal opened with terminalId {terminalSession.TerminalId}");

              var runCommand = true;
              do
              {
                Console.Write($"[{username}]> ");
                var input = Console.ReadLine();

                var command = input.SplitFirstWord(out var parameter);
                if (string.IsNullOrWhiteSpace(command))
                  continue;

                switch (command.ToLowerInvariant())
                {
                  case "quit":
                    {
                      run = false;
                      goto case "exit";
                    }
                  case "exit":
                    {
                      runCommand = false;
                      consoleRef.Tell(new CloseTerminalMsg { TerminalId = terminalSession.TerminalId }, ActorRefs.NoSender);
                      break;
                    }
                  default:
                    {
                      try
                      {
                        var inputMsg = new InputTerminalMsg
                        {
                          TerminalId = terminalSession.TerminalId,
                          TerminalInputId = Guid.NewGuid(),
                          Command = command,
                          Parameter = parameter,
                        };
                        var inputResponse = await consoleRef.Ask(inputMsg).ConfigureAwait(false);
                        switch (inputResponse)
                        {
                          case TerminalInputErrorMsg terminalError:
                            {
                              Console.Error.WriteLine($"[ERROR] {terminalError.ErrorMessage}");
                              break;
                            }
                          case TerminalInputSuccessMsg terminalSuccess:
                            {
                              Console.WriteLine(terminalSuccess.Output);
                              break;
                            }
                        }
                      }
                      catch (Exception ex)
                      {
                        Console.Error.WriteLine($"Terminal error: {ex.Message}");
                      }
                      break;
                    }
                }
              } while (runCommand);
              break;
            }
          default:
            {
              Console.Error.WriteLine($"unknown response: {response}");
              break;
            }
        }
      } while (run);

      await gameHubClientSystem.Terminate();
    }
  }
}
