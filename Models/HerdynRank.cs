using PavelLeagueBot.Enums;
using Newtonsoft.Json;

namespace PavelLeagueBot.Models
{
  public class HerdynRank
  {
    [JsonProperty("tier")]
    public Tier Tier { get; set; }

    [JsonProperty("rank")]
    public Rank Rank { get; set; }

    [JsonProperty("leaguePoints")]
    public int LeaguePoints { get; set; }

    [JsonProperty("wins")]
    public int Wins { get; set; }

    [JsonProperty("losses")]
    public int Losses { get; set; }

    public string GetRank()
      => $"{Tier} {Rank} {LeaguePoints} LP";
  }
}
