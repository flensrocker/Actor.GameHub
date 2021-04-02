using System;
using System.Threading.Tasks;
using Actor.GameHub.Identity.Abtractions;
using Akka.Actor;

namespace Actor.GameHub.Commands
{
  public static partial class CommandsExtrensions
  {
    public static async Task CommandLoopAsync(
      this ActorSystem actorSystem,
      Func<string?, Task> writeAsync,
      Func<string?, Task> errorAsync,
      Func<Task<string?>> readLineAsync)
    {
      var identityManager = await actorSystem
        .ActorSelection(IdentityMetaData.IdentityManagerPath)
        .ResolveOne(TimeSpan.FromSeconds(5.0))
        .ConfigureAwait(false);

      string? username;
      var userId = Guid.Empty;
      var run = true;
      do
      {
        await writeAsync("command: ").ConfigureAwait(false);

        var input = await readLineAsync().ConfigureAwait(false);

        var command = input.SplitFirstWord(out var parameter);
        if (string.IsNullOrWhiteSpace(command))
          continue;

        switch (command.ToLowerInvariant())
        {
          case "?":
          case "help":
            {
              await writeAsync(
                "login username" + Environment.NewLine +
                "logout" + Environment.NewLine +
                "exit" + Environment.NewLine +
                "quit").ConfigureAwait(false);
              break;
            }
          case "exit":
          case "quit":
            {
              run = false;
              break;
            }
          case "logout":
            {
              identityManager.Tell(new UserLogoutMsg { UserId = userId });
              username = null;
              userId = Guid.Empty;
              break;
            }
          case "login" when parameter is not null:
            {
              try
              {
                var loginResponse = await identityManager.Ask(new UserLoginMsg { Username = parameter }, TimeSpan.FromSeconds(5.0));

                switch (loginResponse)
                {
                  case UserLoginSuccessMsg success:
                    {
                      await writeAsync($"Logged in with userId {success.UserId}{Environment.NewLine}").ConfigureAwait(false);
                      username = parameter;
                      userId = success.UserId;
                      break;
                    }
                  case UserLoginErrorMsg error:
                    {
                      await writeAsync($"Login error: {error.ErrorMessage}{Environment.NewLine}").ConfigureAwait(false);
                      break;
                    }
                  case null:
                    {
                      await writeAsync($"No response{Environment.NewLine}").ConfigureAwait(false);
                      break;
                    }
                  default:
                    {
                      await writeAsync($"Unexpected response of type {loginResponse.GetType()}{Environment.NewLine}").ConfigureAwait(false);
                      break;
                    }
                }
              }
              catch (AskTimeoutException)
              {
                await errorAsync($"Login timeout, try again later...{Environment.NewLine}").ConfigureAwait(false);
              }
              break;
            }
          default:
            {
              await errorAsync($"Unknown command: {command} {parameter}{Environment.NewLine}").ConfigureAwait(false);
              break;
            }
        }
      } while (run);
    }
  }
}
