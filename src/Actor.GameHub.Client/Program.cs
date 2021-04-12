using System;
using System.IO;
using System.Threading.Tasks;
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
      var configFile = args is { Length: 1 } ? args[0] : "gamehub-client.akka";
      var config = File.Exists(configFile)
        ? ConfigurationFactory.ParseString(await File.ReadAllTextAsync(configFile))
        : ConfigurationFactory.Default();

      using var gameHubClientSystem = ActorSystem.Create("GameHubClient", config);

      var consoleRef = gameHubClientSystem.ActorOf(ConsoleActor.Props(), "Console");

      var runTerminalLoop = true;
      do
      {
        try
        {
          var prompt = "login: ";
          var username = "";
          do
          {
            Console.Write(prompt);
            username = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(username))
            {
              username = username.Trim();
              break;
            }
          } while (true);

          var openTerminalMsg = new OpenTerminalMsg
          {
            Username = username,
          };
          var terminalResponse = await consoleRef.Ask(openTerminalMsg, TimeSpan.FromSeconds(10.0)).ConfigureAwait(false);

          switch (terminalResponse)
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
            case TerminalOpenSuccessMsg terminalOpenMsg:
              {
                Console.WriteLine($"terminal opened for user {terminalOpenMsg.UserId} with terminalId {terminalOpenMsg.TerminalId}");

                username = terminalOpenMsg.Username;
                prompt = $"[{username}]> ";
                var runCommandLoop = true;
                do
                {
                  Console.Write(prompt);
                  var input = Console.ReadLine();

                  var command = input.SplitFirstWord(out var parameter);
                  if (string.IsNullOrWhiteSpace(command))
                    continue;

                  try
                  {
                    var inputMsg = new InputTerminalMsg
                    {
                      TerminalId = terminalOpenMsg.TerminalId,
                      TerminalInputId = Guid.NewGuid(),
                      Command = command,
                      Parameter = parameter,
                    };
                    var inputResponse = await consoleRef.Ask(inputMsg).ConfigureAwait(false);
                  
                    switch (inputResponse)
                    {
                      case TerminalInputErrorMsg terminalError:
                        {
                          Console.Error.WriteLine($"[ERROR {terminalError.ExitCode}] {terminalError.ErrorMessage}");
                          break;
                        }
                      case TerminalInputSuccessMsg terminalSuccess:
                        {
                          Console.WriteLine(terminalSuccess.Output);
                          break;
                        }
                      case TerminalClosedMsg closedMsg:
                        {
                          Console.WriteLine($"closed with code {closedMsg.ExitCode}");
                          runCommandLoop = false;
                          break;
                        }
                      default:
                        {
                          Console.Error.WriteLine($"[ERROR] unknown response {inputResponse}");
                          runTerminalLoop = false;
                          runCommandLoop = false;
                          break;
                        }
                    }
                  }
                  catch (Exception ex)
                  {
                    Console.Error.WriteLine($"[UNEXPECTED ERROR] {ex.Message}");
                  }
                } while (runTerminalLoop && runCommandLoop);

                consoleRef.Tell(new CloseTerminalMsg { TerminalId = terminalOpenMsg.TerminalId });
                break;
              }
            default:
              {
                Console.Error.WriteLine($"unknown response: {terminalResponse}");
                runTerminalLoop = false;
                break;
              }
          }
        }
        catch (Exception ex)
        {
          await Console.Error.WriteLineAsync($"Cannot open terminal: {ex.Message}");
        }
      } while (runTerminalLoop);

      await gameHubClientSystem.Terminate();
    }
  }
}
