using Newtonsoft.Json;

namespace PavelLeagueBot.Models
{
  public class Game
  {
    [JsonProperty("endOfGameResult")]
    public string EndOfGameResult { get; set; }

    [JsonProperty("gameDuration")]
    public int GameDuration { get; set; }

    [JsonProperty("gameId")]
    public long GameId { get; set; }

    [JsonProperty("participants")]
    public List<LastMatchParticipant> Participants { get; set; }
  }

  public class LastMatchParticipant
  {
    [JsonProperty("assistMePings")]
    public int AssistPings { get; set; }

    [JsonProperty("assists")]
    public int Assists { get; set; }

    [JsonProperty("basicPings")]
    public int BasicPings { get; set; }

    [JsonProperty("champLevel")]
    public int ChampLevel { get; set; }

    [JsonProperty("championName")]
    public string ChampionName { get; set; }

    [JsonProperty("dangerPings")]
    public int DangerPings { get; set; }

    [JsonProperty("deaths")]
    public int Deaths { get; set; }

    [JsonProperty("enemyMissingPings")]
    public int MissingPings { get; set; }

    [JsonProperty("firstBloodKill")]
    public bool FirstBlood { get; set; }

    [JsonProperty("firstTowerKill")]
    public bool FirstTower { get; set; }

    [JsonProperty("gameEndedInEarlySurrender")]
    public bool EarlySurrender { get; set; }

    [JsonProperty("gameEndedInSurrender")]
    public bool Surrender { get; set; }

    [JsonProperty("getBackPings")]
    public int GetBackPings { get; set; }

    [JsonProperty("goldEarned")]
    public int Gold { get; set; }

    [JsonProperty("kills")]
    public int Kills { get; set; }

    [JsonProperty("neutralMinionsKilled")]
    public int NeutralMinions { get; set; }

    [JsonProperty("totalMinionsKilled")]
    public int LaneMinions { get; set; }

    [JsonProperty("puuid")]
    public string Puuid { get; set; }
    //public string riotIdGameName { get; set; }
    //public string riotIdTagline { get; set; }
    //public string summonerName { get; set; }

    [JsonProperty("totalDamageDealtToChampions")]
    public int DamageToChamps { get; set; }

    [JsonProperty("totalDamageTaken")]
    public int DamageTaken { get; set; }

    [JsonProperty("visionScore")]
    public int VisionScore { get; set; }

    [JsonProperty("wardsKilled")]
    public int WardsKilled { get; set; }

    [JsonProperty("wardsPlaced")]
    public int WardsPlaced { get; set; }

    [JsonProperty("win")]
    public bool Win { get; set; }

    public int GetCS()
      => NeutralMinions + LaneMinions;
  }

  public class LastGameInfo
  {
    [JsonProperty("info")]
    public Game LastGame { get; set; }
  }
}
