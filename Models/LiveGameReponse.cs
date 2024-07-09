using Newtonsoft.Json;

namespace PavelLeagueBot.Models
{
  public class Participant
  {
    [JsonProperty("puuid")]
    public string Puuid { get; set; }
    [JsonProperty("teamId")]
    public string TeamID { get; set; }  // 100 = blue side; 200 = red side
    [JsonProperty("championId")]
    public string ChampionID { get; set; }
    [JsonProperty("riotId")]
    public string RiotID { get; set; }
    [JsonProperty("summonerId")]
    public string SummonerID { get; set; }
  }

  public class LiveGameResponse
  {
    [JsonProperty("gameId")]
    public string GameID { get; set; }
    [JsonProperty("participants")]
    public List<Participant> Participants { get; set; }
    [JsonProperty("gameStartTime")]
    public string GameStartTime { get; set; }
    [JsonProperty("gameLength")]
    public string GameLength { get; set; }
  }
}
