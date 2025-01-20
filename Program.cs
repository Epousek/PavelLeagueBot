using System.Text;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using PavelLeagueBot.Connections;
using PavelLeagueBot.Models;
using Serilog;

namespace PavelLeagueBot
{
  internal static class Program
  {
    public static Bot bot;

    private static async Task Main()
    {
      Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateLogger();
      await SecretsConfig.SetConfig().ConfigureAwait(false);

      bot = new Bot();

      //Console.WriteLine(DateTime.Parse("1970/1/1 02:00") + TimeSpan.FromMilliseconds(1720377280763));

      Log.Information($"Bot starting in {(Debugger.IsAttached ? "debug" : "production")} mode");
      var authThread = new Thread(async () => await Authentication.StartValidatingTokenAsync().ConfigureAwait(false));
      var leagueThread = new Thread(async () => await LeagueMatches().ConfigureAwait(false));
      var versionThread = new Thread(async () => await VersionCheck().ConfigureAwait(false));
      authThread.Start();
      leagueThread.Start();
      versionThread.Start();

      await Task.Delay(-1);
    }

    private static async Task LeagueMatches()
    {
      string? currentMatchID = null;
      var riotClient = new RiotApiClient();
      var twitchClient = new TwitchApiClient();

      await Task.Run(async () =>
      {
        await riotClient.SetRank().ConfigureAwait(false);

        while (true)
        {
          Thread.Sleep(60000);
          if (!Bot.isOnline) //i can get rid of this if statement
          {
            //Log.Information("Checking for a game");
            try
            {
              var game = await riotClient.CheckLiveGame().ConfigureAwait(false);
              if (game == null)
              {
                if (currentMatchID != null) //game end
                {
                  currentMatchID = null;

                  Log.Information("Game has ended.");
                  if (await twitchClient.CheckLive("herdyn"))
                  {
                    Log.Information("Stream is live, not sending a message.");
                    await riotClient.SetRank();
                  }
                  else
                  {
                    var lastGame = (await riotClient.GetLastMatchInfo().ConfigureAwait(false)).LastGame;
                    if (lastGame == null)
                    {
                      Log.Error("Didn't get last game info, can't write a message about it.");
                    }
                    else if (!lastGame.GameType.Contains("MATCHED"))
                    {
                      Log.Information("Game was not ranked, not sending a message.");
                    }
                    else
                    {
                      var oldRank = RiotApiClient.herdynRank;
                      await riotClient.SetRank();
                      var newRank = RiotApiClient.herdynRank;
                      LastMatchParticipant pavel = lastGame.Participants.Where(x => x.Puuid == SecretsConfig.Credentials.HerdynRiotID).First();
                      decimal csPerMinute = pavel.GetCS() / (lastGame.GameDuration / 60);
                      var ge = oldRank.Tier == Enums.Tier.DIAMOND ? "Emeraldge" : "Platinumge";

                      StringBuilder builder = new StringBuilder("PAVEL PRÁVĚ ");
                      builder
                        .Append(pavel.Win ? "VYHRÁL OOOO " : $"PROHRÁL {ge} FBCatch ")
                        .Append(pavel.Kills)
                        .Append("/")
                        .Append(pavel.Deaths)
                        .Append("/")
                        .Append(pavel.Assists)
                        .Append(' ')
                        .Append(pavel.ChampionName)
                        .Append(" ⚠️ ")
                        .Append(pavel.GetCS())
                        .Append(" CS (")
                        .Append(Math.Round(csPerMinute, 1))
                        .Append("/min) ⚠️ DAMAGE DEALT: ")
                        .Append(pavel.DamageToChamps)
                        .Append(" ⚠️ DAMAGE TAKEN: ")
                        .Append(pavel.DamageTaken)
                        .Append(" herWard VISION SCORE: ")
                        .Append(pavel.VisionScore)
                        .Append(" herWard ");
                      if (pavel.FirstBlood)
                        builder.Append("FIRST BLOOD 🤯 ");
                      if (pavel.FirstTower)
                        builder.Append("FIRST BREAK 😎 ");
                      if (pavel.Surrender || pavel.EarlySurrender)
                        builder.Append("SURRENDER FailFish ");
                      builder.Append(newRank.GetRank());
                      if (oldRank.Tier == Enums.Tier.UNRANKED || newRank.Tier == Enums.Tier.UNRANKED) //was/is unranked
                      { }
                      else if (oldRank.Rank == newRank.Rank && oldRank.Tier == newRank.Tier) //no demotion/promotion
                      {
                        int diff = oldRank.LeaguePoints - newRank.LeaguePoints;
                        builder
                          .Append(" (")
                          .Append(diff > 0 ? "-" : "+")
                          .Append(diff > 0 ? diff : -1 * diff)
                          .Append(')');
                      }
                      else if (oldRank.Rank < newRank.Rank && oldRank.Tier == newRank.Tier)  //promoted rank
                      {
                        int diff = (100 - oldRank.LeaguePoints) + newRank.LeaguePoints;
                        builder
                          .Append(" (promoted, +")
                          .Append(diff)
                          .Append(')');
                      }
                      else if (oldRank.Rank > newRank.Rank && oldRank.Tier == newRank.Tier) //demoted rank
                      {
                        int diff = oldRank.LeaguePoints + (100 - newRank.LeaguePoints);
                        builder
                          .Append(" (demoted, -")
                          .Append(diff)
                          .Append(')');
                      }
                      else if (oldRank.Tier < newRank.Tier) //promoted tier
                      {
                        int diff = (100 - oldRank.LeaguePoints) + newRank.LeaguePoints;
                        builder
                          .Append(" (PROMOTED!! OOOO +")
                          .Append(diff)
                          .Append(')');
                      }
                      else if (oldRank.Tier > newRank.Tier) //demoted tier
                      {
                        int diff = oldRank.LeaguePoints + (100 - newRank.LeaguePoints);
                        builder
                          .Append(" (DEMOTED!! forsenLaughingAtYou -")
                          .Append(diff)
                          .Append(')');
                      }

                      Bot.WriteMessage(builder.ToString());
                    }
                  }
                }
              }
              else if (game.GameID == currentMatchID) //game continues
              { }
              else //game start
              {
                Log.Information("Game has started.");
                await riotClient.SetRank();
                currentMatchID = game.GameID;
                var rank = RiotApiClient.herdynRank;

                if (await twitchClient.CheckLive("herdyn"))
                {
                  Log.Information("Stream is live, not sending a message.");
                }
                else if (!game.GameType.Contains("MATCHED"))
                {
                  Log.Information("Not playing ranked, not sending a message.");
                }
                else
                {
                  Participant pavel = game.Participants.Where(x => x.Puuid == SecretsConfig.Credentials.HerdynRiotID).ToList().First();
                  string champion = RiotApiClient.champions.Where(x => x.ID == pavel.ChampionID).ToList().First().Name;
                  string side = pavel.TeamID == "100" ? "🟦 BLUE SIDE 🟦" : "🟥 RED SIDE 🟥";

                  StringBuilder builder = new StringBuilder("DinkDonk PAVEL PRÁVĚ ZAPNUL HRU DinkDonk ");
                  builder
                    .Append(champion.ToUpper())
                    .Append(pavel.TeamID == "100" ? " \U0001f7e6 BLUE SIDE \U0001f7e6 " : " \U0001f7e5 RED SIDE \U0001f7e5 ");
                  if (rank.Tier != Enums.Tier.UNRANKED)
                    builder
                      .Append(rank.GetRank())
                      .Append(" LETHIMCOOK");

                  Bot.WriteMessage(builder.ToString());
                }
              }
            }
            catch (Exception)
            {
              if (currentMatchID != null)
                Bot.WriteMessage("game has ended");
              continue;
            }
          }
          else
          {
            //Log.Information("pavel is streaming");
          }
        }
      });
    }

    private static async Task VersionCheck() // set champions list if version changed
    {
      var client = new RiotApiClient();

      while (true)
      {
        string? lastVersion = await client.GetLastVersion();
        if (lastVersion != null && RiotApiClient.gameVersion != lastVersion)
        {
          RiotApiClient.gameVersion = lastVersion;
          await client.SetChampionList();
        }

        Thread.Sleep(TimeSpan.FromDays(1));
      }
    }

    //private static void Disconnect()
    //{
    //  Task.Run(() =>
    //  {
    //    while (true)
    //    {
    //      Thread.Sleep(20000);
    //      Bot._client.Disconnect();
    //    }
    //  });
    //}
  }
}
