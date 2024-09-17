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
    private bool _connected;
    private static TwitchClient _client;
    private static string connectedChannel = Debugger.IsAttached ? "donkousek" : "herdyn";

    public static bool isOnline;

    public Bot()
    {

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

    public static void WriteMessage(string message)
      => _client.SendMessage(connectedChannel, message);

    private async void Client_OnDisconnected(object? sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
    {
      _connected = false;
      Log.Error("he disconnected");      //RANDOM DISCONNECTS!?!?..,"!"?
      await Reconnect();
    }

    private void Client_OnError(object? sender, TwitchLib.Communication.Events.OnErrorEventArgs e)
      => Log.Error($"TwitchLib error: {0}: {1}", e.Exception, e.Exception.Message);

    private void Client_OnConnected(object? sender, TwitchLib.Client.Events.OnConnectedArgs e)
    {
      _connected = true;
      Log.Information("{username} connected.", e.BotUsername);
    }

    private void Client_OnJoinedChannel(object? sender, TwitchLib.Client.Events.OnJoinedChannelArgs e)
      => Log.Information("Joined {channel}.", e.Channel);

    private async Task Reconnect()
    {
      int timeout = 1;

      try
      {
        _client.Disconnect();
      }
      catch (Exception)
      { }

      while (!_connected)
      {
        if (timeout > 64)
          throw new Exception("Too many reconnect attempts, we're shutting down...");

        timeout = 2 * timeout;
        Log.Information("Trying to (re)connect, next attempt in {time} minutes", timeout);

        _client.Connect();
        Thread.Sleep(TimeSpan.FromMinutes(timeout));
      }
    }
  }
}
