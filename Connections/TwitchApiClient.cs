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
    private readonly RestClient _clientID;
    private readonly RestClient _clientAPI;

    public TwitchApiClient()
    {
      _clientID = new RestClient("https://id.twitch.tv/");
      _clientAPI = new RestClient("https://api.twitch.tv/");
    }

    public async Task<bool> CheckLive(string channel) // MAYBE SEPERATE THIS INTO IT'S OWN CLASS
    {
      var request = new RestRequest("helix/streams");

      request.AddHeader("Authorization", "Bearer " + SecretsConfig.Credentials.AccessToken);
      request.AddHeader("Client-Id", SecretsConfig.Credentials.ClientId);
      request.AddParameter("user_login", channel);
      request.AddParameter("type", "live");

      var response = await _clientAPI.ExecuteAsync(request);
      if (response.IsSuccessful)
      {
        JObject content = JObject.Parse(response.Content);
        JToken dataField = content["data"];

        if (dataField != null && dataField.HasValues)
        {
          return true;
        }
        else
        {
          return false;
        }
      }
      else
      {
        Log.Error("Error getting stream from twitch: {message}", response.StatusDescription);
        return true; // better not write in chat in case of stream being live i guess
      }
    }

    public async Task ValidateAccessToken()
    {
      Log.Information("Trying to validate app access token");
      var request = new RestRequest("oauth2/validate");

      request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
      request.AddHeader("Accept", "application/json");
      request.AddHeader("Authorization", "Bearer " + SecretsConfig.Credentials.AccessToken);

      var response = await _clientID.ExecuteAsync(request).ConfigureAwait(false);
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

      var response = await _clientID.ExecuteAsync(request).ConfigureAwait(false);
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