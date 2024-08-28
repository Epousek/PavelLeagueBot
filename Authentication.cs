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
        _client ??= new TwitchApiClient();
        while (true)
        {
          int expiresIn = await Task.Run(RefreshAccessToken).ConfigureAwait(false);
          Thread.Sleep(TimeSpan.FromSeconds(expiresIn) - TimeSpan.FromMinutes(10));
        }
      }).ConfigureAwait(false);
    }

    private static async Task<int> RefreshAccessToken()
    {
      return await _client.RefreshTokens().ConfigureAwait(false);
    }
  }
}
