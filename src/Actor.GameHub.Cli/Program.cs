using System;
using System.IO;
using System.Threading.Tasks;
using Actor.GameHub.Identity.Abstractions;
using Actor.GameHub.Terminal;
using Actor.GameHub.Terminal.Abstractions;
using Actor.GameHub.Terminal.Abtractions;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Configuration;

namespace Actor.GameHub.Cli
{
  class Program
  {
    static async Task Main(string[] args)
    {
      var config = File.Exists("app.config")
        ? ConfigurationFactory.ParseString(await File.ReadAllTextAsync("app.config"))
        : ConfigurationFactory.Default();

      var gamehubSystem = ActorSystem.Create("GameHub", config);

      var username = "";
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

      var mediator = DistributedPubSub.Get(gamehubSystem).Mediator;
      var openTerminalMsg = new OpenTerminalMsg
      {
        LoginUser = new LoginUserMsg
        {
          Username = username,
        },
      };
      var sendOpen = new Send(TerminalMetadata.TerminalPath, openTerminalMsg);
      var response = await mediator.Ask(sendOpen, TimeSpan.FromSeconds(10.0)).ConfigureAwait(false);

      switch (response)
      {
        case TerminalOpenSuccessMsg terminalSession:
          {
            Console.WriteLine($"terminal opened with terminalId {terminalSession.TerminalId}");

            var run = true;
            do
            {
              Console.Write("command: ");
              var input = Console.ReadLine();

              var command = input.SplitFirstWord(out var parameter);
              if (string.IsNullOrWhiteSpace(command))
                continue;

              switch (command.ToLowerInvariant())
              {
                case "exit":
                case "quit":
                  {
                    run = false;
                    terminalSession.TerminalRef.Tell(new CloseTerminalMsg { TerminalId = terminalSession.TerminalId }, ActorRefs.NoSender);
                    break;
                  }
                default:
                  {
                    try
                    {
                      var inputMsg = new InputTerminalMsg
                      {
                        TerminalId = terminalSession.TerminalId,
                        Command = command,
                        Parameter = parameter,
                      };
                      response = await terminalSession.TerminalRef.Ask(inputMsg, TimeSpan.FromSeconds(10.0)).ConfigureAwait(false);
                      if (response is TerminalOutputMsg output)
                        Console.WriteLine(output.Output);
                    }
                    catch (Exception ex)
                    {
                      Console.Error.WriteLine($"Terminal not found, exiting: {ex.Message}");
                      run = false;
                    }
                    break;
                  }
              }
            } while (run);
            break;
          }
        case TerminalOpenErrorMsg terminalErrorMsg:
          {
            Console.Error.WriteLine($"Terminal error, exiting: {terminalErrorMsg.ErrorMessage}");
            break;
          }
        default:
          {
            Console.Error.WriteLine($"unknown response, exiting: {response}");
            break;
          }
      }

      await gamehubSystem.Terminate();
    }
  }
}
