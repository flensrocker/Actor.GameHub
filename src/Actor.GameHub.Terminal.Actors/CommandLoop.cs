using System;
using System.Threading.Tasks;
using Actor.GameHub.Identity.Abstractions;
using Akka.Actor;

namespace Actor.GameHub.Terminal
{
  public static partial class CommandsExtrensions
  {
    public static async Task CommandLoopAsync(
      this ActorSystem actorSystem,
      Func<string?, Task> writeAsync,
      Func<string?, Task> errorAsync,
      Func<Task<string?>> readLineAsync,
      string gameServerAddress = "")
    {
      var identityRef = await actorSystem
        .ActorSelection($"{gameServerAddress}{IdentityMetadata.IdentityPath}")
        .ResolveOne(TimeSpan.FromSeconds(5.0))
        .ConfigureAwait(false);

      UserLoginSuccessMsg? loginSession = null;
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
          case "logout" when loginSession is not null:
            {
              loginSession.UserLogin.Tell(new LogoutUserMsg { });
              loginSession = null;
              break;
            }
          case "login" when loginSession is null && parameter is not null:
            {
              try
              {
                var loginResponse = await identityRef.Ask(new LoginUserMsg { Username = parameter }, TimeSpan.FromSeconds(5.0)).ConfigureAwait(false);

                switch (loginResponse)
                {
                  case UserLoginSuccessMsg success:
                    {
                      loginSession = success;
                      await writeAsync($"Logged in with loginId {success.UserLoginId}{Environment.NewLine}").ConfigureAwait(false);
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
              await errorAsync($"Unexpected command: {command} {parameter}{Environment.NewLine}").ConfigureAwait(false);
              break;
            }
        }
      } while (run);

      if (loginSession is not null)
        loginSession.UserLogin.Tell(new LogoutUserMsg { });
    }
  }
}
