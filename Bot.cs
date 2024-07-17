using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Services;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace PavelLeagueBot
{
  internal class Bot
  {
    private static TwitchClient _client;
    private static string connectedChannel = Debugger.IsAttached ? "donkousek" : "herdyn"; //change to herdyn
    private TwitchAPI API;
    private LiveStreamMonitorService Monitor;

    public static bool isOnline;

    public Bot()
    {
      Task.Run(async () => await ConfigLiveMonitorAsync());

      var creds = new ConnectionCredentials(SecretsConfig.Credentials.Username, SecretsConfig.Credentials.AccessToken);
      var clientOptions = new ClientOptions
      {
        MessagesAllowedInPeriod = 750,
        ThrottlingPeriod = TimeSpan.FromSeconds(30)
      };
      var wsClient = new WebSocketClient(clientOptions);
      _client = new TwitchClient(wsClient);
      _client.Initialize(creds, connectedChannel);

      _client.OnJoinedChannel += Client_OnJoinedChannel;
      _client.OnConnected += Client_OnConnected;
      _client.OnError += Client_OnError;
      _client.OnDisconnected += Client_OnDisconnected;

      try
      {
        _client.Connect();
      }
      catch (Exception e)
      {
        Log.Error("Couldn't connet to chat: {message}", e.Message);
        Reconnect();
      }
    }

    private async Task ConfigLiveMonitorAsync()
    {
      API = new TwitchAPI();
      API.Settings.ClientId = SecretsConfig.Credentials.ClientId;
      API.Settings.AccessToken = SecretsConfig.Credentials.AccessToken;
      Monitor = new LiveStreamMonitorService(API, 60);

      Monitor.OnStreamOnline += Monitor_OnStreamOnline;
      Monitor.OnStreamOffline += Monitor_OnStreamOffline;
      Monitor.OnServiceStarted += Monitor_OnServiceStarted;
      Monitor.OnServiceStopped += Monitor_OnServiceStopped;

      Monitor.SetChannelsByName(["herdyn"]);
      Monitor.Start();

      await Task.Delay(-1);
    }

    private void Monitor_OnServiceStopped(object? sender, TwitchLib.Api.Services.Events.OnServiceStoppedArgs e)
      => Log.Error("Live stream monitor stopped monitoring for some reason.");

    private void Monitor_OnServiceStarted(object? sender, TwitchLib.Api.Services.Events.OnServiceStartedArgs e)
      => Log.Information("Live stream monitor started monitoring.");

    private void Monitor_OnStreamOffline(object? sender, TwitchLib.Api.Services.Events.LiveStreamMonitor.OnStreamOfflineArgs e)
    {
      isOnline = false;
      Log.Information("stream offline");
    }

    private void Monitor_OnStreamOnline(object? sender, TwitchLib.Api.Services.Events.LiveStreamMonitor.OnStreamOnlineArgs e)
    {
      isOnline = true;
      Log.Information("stream live");
    }

    public static void WriteMessage(string message)
      => _client.SendMessage(connectedChannel, message);

    private async void Client_OnDisconnected(object? sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
    {
      Log.Error("he disconnected");      //RANDOM DISCONNECTS!?!?..,"!"?
      await Reconnect();
    }

    private void Client_OnError(object? sender, TwitchLib.Communication.Events.OnErrorEventArgs e)
      => Log.Error($"TwitchLib error: {0}: {1}", e.Exception, e.Exception.Message);

    private void Client_OnConnected(object? sender, TwitchLib.Client.Events.OnConnectedArgs e)
    {
      Log.Information("{username} connected.", e.BotUsername);
    }

    private void Client_OnJoinedChannel(object? sender, TwitchLib.Client.Events.OnJoinedChannelArgs e)
      => Log.Information("Joined {channel}.", e.Channel);

    private async Task Reconnect()
    {
      int timeout = 1;

      while (!_client.IsConnected)
      {
        timeout = 2 * timeout;
        Log.Information("Trying to (re)connect, next attempt in {time} minutes", timeout);

        _client.Connect();
        Thread.Sleep(TimeSpan.FromMinutes(timeout));

        if (timeout > 64)
          throw new Exception("Too many reconnect attempts, we're shutting down...");
      }
    }
  }
}
