using System;
using System.Threading.Tasks;
using RestSharp;
using Serilog;
using Newtonsoft.Json.Linq;
using PavelLeagueBot.Models;

namespace PavelLeagueBot.Connections
{
  public class TwitchApiClient
  {
    private readonly RestClient _client;

    public TwitchApiClient()
    {
      _client = new RestClient("https://id.twitch.tv/");
    }

    public async Task ValidateAccessToken()
    {
      Log.Information("Trying to validate app access token");
      var request = new RestRequest("oauth2/validate");

      request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
      request.AddHeader("Accept", "application/json");
      request.AddHeader("Authorization", "Bearer " + SecretsConfig.Credentials.AccessToken);

      var response = await _client.ExecuteAsync(request).ConfigureAwait(false);
      if (response.IsSuccessful)
      {
        var responseJson = JObject.Parse(response.Content);
        var validationResponse = responseJson.ToObject<TokenValidationResponse>();
        if (validationResponse.ExpiresIn < 5400)
        {
          Log.Information("Token is about to expire, refreshing.");
          await RefreshTokens().ConfigureAwait(false);
        }
        else
        {
          Log.Information("Token expires in about {expiresIn} hours", TimeSpan.FromSeconds(validationResponse.ExpiresIn).TotalHours);
        }
      }
      else
      {
        Log.Error("Couldn't validate: {statusDescription}", response.StatusDescription);
      }
    }

    private async Task RefreshTokens()
    {
      Log.Information("Trying to refresh token.");

      var request = new RestRequest("oauth2/token", Method.Post);

      request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
      request.AddHeader("Accept", "application/json");
      request.AddParameter("client_id", SecretsConfig.Credentials.ClientId);
      request.AddParameter("client_secret", SecretsConfig.Credentials.Secret);
      request.AddParameter("grant_type", "refresh_token");
      request.AddParameter("refresh_token", SecretsConfig.Credentials.RefreshToken);

      var response = await _client.ExecuteAsync(request).ConfigureAwait(false);
      if (response.IsSuccessful)
      {
        Log.Information("Refreshed tokens.");

        var responseJson = JObject.Parse(response.Content);
        var newToken = responseJson.ToObject<AppAccessToken>();
        await SecretsConfig.SetToken(newToken).ConfigureAwait(false);
      }
      else
      {
        Log.Error(response.ErrorException, "Failed to refresh token: {statusDescription}.", response.StatusDescription);
      }
    }
  }
}