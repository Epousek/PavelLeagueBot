using System;
using System.Threading.Tasks;
using RestSharp;
using Serilog;
using Newtonsoft.Json.Linq;
using PavelLeagueBot.Models;
using PavelLeagueBot.Enums;

namespace PavelLeagueBot.Connections
{
  internal class RiotApiClient
  {
    private static readonly RestClient _clientEUROPE = new RestClient("https://europe.api.riotgames.com/");
    private static readonly RestClient _clientEUW = new RestClient("https://euw1.api.riotgames.com/");
    private static readonly RestClient _clientDragon = new RestClient("https://ddragon.leagueoflegends.com/");

    public static string gameVersion;
    public static List<Champion> champions;
    public static HerdynRank herdynRank;

    public RiotApiClient()
    {
      //_clientEUROPE ??= new RestClient("https://europe.api.riotgames.com/");
      //_clientEUW ??= new RestClient("https://euw1.api.riotgames.com/");
      //_clientDragon ??= new RestClient("https://ddragon.leagueoflegends.com/");
    }

    public async Task<LiveGameResponse?> CheckLiveGame()
    {
      //Log.Information("Checking if there is a game going on.");

      var request = new RestRequest($"lol/spectator/v5/active-games/by-summoner/{SecretsConfig.Credentials.HerdynRiotID}");
      request.AddHeader("X-Riot-Token", SecretsConfig.Credentials.RiotApiKey);
      var response = await _clientEUW.ExecuteAsync(request).ConfigureAwait(false);

      if (response.IsSuccessful)
      {
        //Log.Information("Pejvl is playing!!");
        var responseJson = JObject.Parse(response.Content);
        return responseJson.ToObject<LiveGameResponse>();
      }
      else
      {
        if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
          Log.Error("Error {code} when looking for live game: {message}", response.StatusCode, response.Content);
        return null;
      }
    }

    public async Task<LastGameInfo?> GetLastMatchInfo()
    {
      string? matchID = await GetLastMatchID();
      if (matchID != null)
      {
        var request = new RestRequest($"lol/match/v5/matches/{matchID}");
        request.AddHeader("X-Riot-Token", SecretsConfig.Credentials.RiotApiKey);
        var response = await _clientEUROPE.ExecuteAsync(request).ConfigureAwait(false);

        if (response.IsSuccessful)
        {
          var responseJson = JObject.Parse(response.Content);
          return responseJson.ToObject<LastGameInfo>();
        }
        else
        {
          Log.Error("Couldn't get last match info: {message}", response.Content);
          return null;
        }
      }
      else
      {
        Log.Error("No match ID, can't get last match info.");
        return null;
      }
    }

    public async Task<string?> GetLastMatchID()
    {
      var request = new RestRequest($"lol/match/v5/matches/by-puuid/{SecretsConfig.Credentials.HerdynRiotID}/ids?start=0&count=1");
      request.AddHeader("X-Riot-Token", SecretsConfig.Credentials.RiotApiKey);
      var response = await _clientEUROPE.ExecuteAsync(request).ConfigureAwait(false);

      if (response.IsSuccessful)
      {
        return response.Content.ToString().Replace("\"", "").Replace("[", "").Replace("]", ""); //refactor this when im not lazy
      }
      else
      {
        Log.Error("Couldn't get last match ID: {message}", response.Content);
        return null;
      }
    }

    public async Task<string?> GetLastVersion()
    {
      Log.Information("Getting latest game version.");
      var request = new RestRequest("api/versions.json", Method.Get);
      var response = await _clientDragon.ExecuteAsync(request);

      if (response.IsSuccessful)
      {
        var jsonArray = JArray.Parse(response.Content);
        string currentVersion = jsonArray[0].ToString();
        Log.Information("Current game version: {version}", currentVersion);

        return currentVersion;
      }
      else
      {
        Log.Error("Error getting last version: {message}", response.Content);
        return null;
      }
    }

    public async Task SetRank()
    {
      Log.Information("Setting herdyn's rank");

      var request = new RestRequest($"lol/league/v4/entries/by-summoner/{SecretsConfig.Credentials.HerdynSummonerID}");
      request.AddHeader("X-Riot-Token", SecretsConfig.Credentials.RiotApiKey);
      var response = await _clientEUW.ExecuteAsync(request);

      if (response.IsSuccessful)
      {
        var responseJson = JArray.Parse(response.Content);
        var rank = new HerdynRank();
        //rank.Rank = responseJson[0]["rank"].ToString();
        //rank.Tier = responseJson[0]["tier"].ToString();

        if (!responseJson.HasValues)
        {
          rank.Tier = Tier.UNRANKED;
          rank.Rank = Rank.I;
          rank.LeaguePoints = 0;
          rank.Wins = 0;
          rank.Losses = 0;
        }
        else
        {
          string rankStr = responseJson[0]["rank"].ToString();
          string tierStr = responseJson[0]["tier"].ToString();

          if (Enum.TryParse<Rank>(rankStr, true, out Rank rankEnum) && Enum.TryParse<Tier>(tierStr, true, out Tier tierEnum))
          {
            rank.Rank = rankEnum;
            rank.Tier = tierEnum;
          }
          else
          {
            Log.Error("Failed to parse rank/tier into enum.");
          }

          rank.LeaguePoints = (int)responseJson[0]["leaguePoints"];
          rank.Losses = (int)responseJson[0]["losses"];
          rank.Wins = (int)responseJson[0]["wins"];
        }

        herdynRank = rank;
        Log.Information("Successfully set herdyn's rank.");
      }
      else
      {
        Log.Error("Couldn't get herdyn's rank: {message}", response.Content);
      }
    }

    public async Task SetChampionList()
    {
      Log.Information("Getting list of champions.");
      var request = new RestRequest($"cdn/{gameVersion}/data/en_US/champion.json", Method.Get);
      var response = await _clientDragon.ExecuteAsync(request);

      if (response.IsSuccessful)
      {
        Log.Information("Setting list of champions.");
        champions = new List<Champion>();
        var jsonObject = JObject.Parse(response.Content);
        var championsData = jsonObject["data"];

        foreach (var champion in championsData)
        {
          Champion champ = new Champion
          {
            ID = champion.First["key"].ToString(),
            Name = champion.First["name"].ToString()
          };

          champions.Add(champ);
        }
      }
      else
      {
        Log.Error("Failed getting and setting list of champions: {message}", response.Content.ToString());
      }
    }
  }
}
