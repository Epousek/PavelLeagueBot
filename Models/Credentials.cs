using Newtonsoft.Json;

namespace PavelLeagueBot.Models
{
  internal class Credentials
  {
    [JsonProperty("username")]
    public string Username { get; set; }

    [JsonProperty("client_id")]
    public string ClientId { get; set; }

    [JsonProperty("secret")]
    public string Secret { get; set; }

    [JsonProperty("access_token")]
    public string AccessToken { get; set; }

    [JsonProperty("refresh_token")]
    public string RefreshToken { get; set; }

    [JsonProperty("herdyn_riot_id")]
    public string HerdynRiotID { get; set; }

    [JsonProperty("herdyn_summoner_id")]
    public string HerdynSummonerID { get; set; }

    [JsonProperty("riot_api_key")]
    public string RiotApiKey { get; set; }
  }
}
