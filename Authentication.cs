using System;
using System.Threading;
using System.Threading.Tasks;
using PavelLeagueBot.Connections;

namespace PavelLeagueBot
{
  internal static class Authentication
  {
    private static TwitchApiClient _client;

    public static async Task StartValidatingTokenAsync()
    {
      await Task.Run(async () =>
      {
        while (true)
        {
          await Task.Run(ValidateAccessToken).ConfigureAwait(false);
          Thread.Sleep(TimeSpan.FromHours(1));
        }
      }).ConfigureAwait(false);
    }

    private static async Task ValidateAccessToken()
    {
      _client ??= new TwitchApiClient();
      await _client.ValidateAccessToken().ConfigureAwait(false);
    }
  }
}
