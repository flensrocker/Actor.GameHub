using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Actor.GameHub.UserManager;
using Actor.GameHub.UserManager.Messages;
using Akka.Actor;

namespace Actor.GameHub
{
  class Program
  {
    static async Task Main(string[] args)
    {
      var gamehubSystem = ActorSystem.Create("GameHub");

      var userManager = gamehubSystem.ActorOf(UserManagerActor.Props(), "UserManager");

      var userManagerSelection = gamehubSystem.ActorSelection("/user/UserManager");
      var userManagerRef = await userManagerSelection.ResolveOne(TimeSpan.FromSeconds(5.0));
      System.Diagnostics.Debug.Assert(userManager == userManagerRef);

      var run = true;
      var users = new Dictionary<string, Guid>();
      do
      {
        Console.Write("command: ");
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
          continue;

        var spaceIndex = input.IndexOf(" ");
        var command = spaceIndex < 0
          ? input
          : input.Substring(0, spaceIndex);
        var parameter = spaceIndex < 0
          ? ""
          : input.Substring(spaceIndex + 1);

        switch (command.ToLowerInvariant())
        {
          case "?":
          case "help":
            {
              Console.WriteLine("login username");
              Console.WriteLine("logout username");
              Console.WriteLine("exit");
              Console.WriteLine("quit");
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
              if (users.TryGetValue(parameter, out var userId))
              {
                userManager.Tell(new UserLogoutMsg { UserId = userId });
                users.Remove(parameter);
              }
              break;
            }
          case "login":
            {
              try
              {
                var loginResponse = await userManager.Ask(new UserLoginMsg { Username = parameter }, TimeSpan.FromSeconds(5.0));

                switch (loginResponse)
                {
                  case UserLoginSuccessMsg success:
                    {
                      Console.WriteLine($"Logged in with userId {success.UserId}");
                      users.Add(parameter, success.UserId);
                      break;
                    }
                  case UserLoginErrorMsg error:
                    {
                      Console.WriteLine($"Login error: {error.ErrorMessage}");
                      break;
                    }
                  case null:
                    {
                      Console.WriteLine("No response");
                      break;
                    }
                  default:
                    {
                      Console.WriteLine($"Unexpected response of type {loginResponse.GetType()}");
                      break;
                    }
                }
              }
              catch (AskTimeoutException)
              {
                Console.Error.WriteLine("Login timeout, try again later...");
              }
              break;
            }
        }
      } while (run);

      foreach (var user in users)
        userManager.Tell(new UserLogoutMsg { UserId = user.Value });

      await gamehubSystem.Terminate();
    }
  }
}
