using Newtonsoft.Json;

namespace PavelLeagueBot.Models
{
  public class TokenValidationResponse
  {
    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }
  }
}
