using Newtonsoft.Json;

namespace PavelLeagueBot.Models
{
  internal class AppAccessToken
  {
    [JsonProperty("access_token")]
    public string AccessToken { get; set; }

    [JsonProperty("refresh_token")]
    public string RefreshToken { get; set; }

    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }
  }
}
