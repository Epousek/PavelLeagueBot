﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using PavelLeagueBot.Connections;
using Serilog;
using PavelLeagueBot.Models;
using System.Text;
using RestSharp;

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

      await Task.Run(async () =>
      {
        await riotClient.SetRank().ConfigureAwait(false);

        while (true)
        {
          Thread.Sleep(60000);
          if (!Bot.isOnline)
          {
            try
            {
              var game = await riotClient.CheckLiveGame().ConfigureAwait(false);
              if (game == null)
              {
                if (currentMatchID != null) //game end
                {
                  Log.Information("Game has ended.");
                  currentMatchID = null;
                  var lastGame = (await riotClient.GetLastMatchInfo().ConfigureAwait(false)).LastGame;

                  if (lastGame == null)
                  {
                    Log.Error("Didn't get last game info, can't write a message about it.");
                  }
                  else
                  {
                    var oldRank = RiotApiClient.herdynRank;
                    await riotClient.SetRank();
                    var newRank = RiotApiClient.herdynRank;
                    LastMatchParticipant pavel = lastGame.Participants.Where(x => x.Puuid == SecretsConfig.Credentials.HerdynRiotID).First();
                    decimal csPerMinute = pavel.GetCS() / lastGame.GameDuration * 60;

                    StringBuilder builder = new StringBuilder("PAVEL PRÁVĚ ");
                    builder
                      .Append(pavel.Win ? "VYHRÁL OOOO " : "PROHRÁL Emeraldge FBCatch ")
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
                      .Append(Math.Round(csPerMinute, 1))   //not tested
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
                    builder
                      .Append(newRank.GetRank());
                    if (oldRank.Rank == newRank.Rank && oldRank.Tier == newRank.Tier) //no demotion/promotion
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
                        .Append(" (PROMOTED!! +")
                        .Append(diff)
                        .Append(')');
                    }
                    else if (oldRank.Tier > newRank.Tier) //demoted tier
                    {
                      int diff = oldRank.LeaguePoints + (100 - newRank.LeaguePoints);
                      builder
                        .Append(" (DEMOTED!! -")
                        .Append(diff)
                        .Append(')');
                    }

                    Bot.WriteMessage(builder.ToString());
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
                Participant pavel = game.Participants.Where(x => x.Puuid == SecretsConfig.Credentials.HerdynRiotID).ToList().First();
                string champion = RiotApiClient.champions.Where(x => x.ID == pavel.ChampionID).ToList().First().Name;
                string side = pavel.TeamID == "100" ? "🟦 BLUE SIDE 🟦" : "🟥 RED SIDE 🟥";

                StringBuilder builder = new StringBuilder("DinkDonk PAVEL PRÁVĚ ZAPNUL HRU DinkDonk ");
                builder
                  .Append(champion.ToUpper())
                  .Append(pavel.TeamID == "100" ? " \U0001f7e6 BLUE SIDE \U0001f7e6 " : " \U0001f7e5 RED SIDE \U0001f7e5 ")
                  .Append(RiotApiClient.herdynRank.GetRank())
                  .Append(" LETHIMCOOK");

                Bot.WriteMessage(builder.ToString());
              }
            }
            catch (Exception)
            {
              if (currentMatchID != null)
                Bot.WriteMessage("game has ended");
              continue;
            }
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