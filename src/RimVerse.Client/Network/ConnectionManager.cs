using System;
using System.Threading;
using Newtonsoft.Json;
using RimVerse.Client.Core;
using Verse;

namespace RimVerse.Client.Network
{
    public class ConnectionManager
    {
        public static ConnectionManager Instance { get; private set; }

        public ApiClient Api { get; private set; }
        public RimVerseWebSocket WebSocket { get; private set; }
        public bool IsConnected => WebSocket != null && WebSocket.IsConnected;

        public event Action OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<string, string, string> OnChatMessageReceived;
        public event Action<long> OnWorldClockSync;

        public ConnectionManager()
        {
            Instance = this;
        }

        public void Connect()
        {
            var settings = RimVerseMod.Settings;
            if (string.IsNullOrEmpty(settings.ServerUrl))
            {
                Log.Error("[RimVerse] Server URL is empty. Configure it in mod settings.");
                return;
            }

            Api = new ApiClient(settings.ServerUrl);

            try
            {
                if (string.IsNullOrEmpty(settings.SavedToken))
                {
                    Login();
                }
                else
                {
                    Api.SetAuthToken(settings.SavedToken);
                    RimVerseMod.AuthToken = settings.SavedToken;
                    if (Guid.TryParse(settings.SavedPlayerId, out var pid))
                        RimVerseMod.LocalPlayerId = pid;
                }

                ConnectWebSocket();
            }
            catch (Exception ex)
            {
                Log.Error($"[RimVerse] Connection failed: {ex.Message}");
                settings.SavedToken = "";
                settings.SavedPlayerId = "";

                try
                {
                    Login();
                    ConnectWebSocket();
                }
                catch (Exception ex2)
                {
                    Log.Error($"[RimVerse] Login also failed: {ex2.Message}");
                }
            }
        }

        private void Login()
        {
            var settings = RimVerseMod.Settings;
            if (string.IsNullOrEmpty(settings.Username) || string.IsNullOrEmpty(settings.Password))
            {
                Log.Error("[RimVerse] Username or password is empty.");
                return;
            }

            Log.Message("[RimVerse] Logging in...");

            AuthResponse response;
            try
            {
                response = Api.Post<AuthResponse>("/api/auth/login", new
                {
                    Username = settings.Username,
                    Password = settings.Password
                });
            }
            catch
            {
                Log.Message("[RimVerse] Login failed, attempting registration...");
                response = Api.Post<AuthResponse>("/api/auth/register", new
                {
                    Username = settings.Username,
                    Password = settings.Password
                });
            }

            settings.SavedToken = response.Token;
            settings.SavedPlayerId = response.PlayerId;
            Api.SetAuthToken(response.Token);
            RimVerseMod.AuthToken = response.Token;
            RimVerseMod.LocalPlayerName = response.DisplayName;

            if (Guid.TryParse(response.PlayerId, out var pid))
                RimVerseMod.LocalPlayerId = pid;

            Log.Message($"[RimVerse] Logged in as {response.DisplayName}");
        }

        private void ConnectWebSocket()
        {
            var settings = RimVerseMod.Settings;
            WebSocket = new RimVerseWebSocket(settings.ServerUrl, settings.SavedToken);

            WebSocket.OnConnected += () =>
            {
                RimVerseMod.IsConnected = true;
                Log.Message("[RimVerse] Fully connected to server");
                OnConnected?.Invoke();
            };

            WebSocket.OnDisconnected += reason =>
            {
                RimVerseMod.IsConnected = false;
                Log.Warning($"[RimVerse] Disconnected: {reason}");
                OnDisconnected?.Invoke(reason);
            };

            WebSocket.OnMessageReceived += (type, data) =>
            {
                HandleServerMessage(type, data);
            };

            WebSocket.Connect();
        }

        public void Disconnect()
        {
            WebSocket?.Disconnect();
            RimVerseMod.IsConnected = false;
            RimVerseMod.AuthToken = null;
            Log.Message("[RimVerse] Disconnected from server");
            OnDisconnected?.Invoke("Manual disconnect");
        }

        public void SendChat(string channel, string content)
        {
            WebSocket?.SendChat(channel, content);
        }

        private void HandleServerMessage(string type, string rawData)
        {
            try
            {
                var envelope = JsonConvert.DeserializeObject<ServerMessage>(rawData);
                if (envelope == null) return;

                switch (envelope.Type)
                {
                    case "ReceiveChatMessage":
                        var chat = JsonConvert.DeserializeObject<ChatMessageData>(envelope.Data);
                        if (chat != null)
                            OnChatMessageReceived?.Invoke(chat.SenderName, chat.Channel, chat.Content);
                        break;

                    case "WorldClockSync":
                        var clock = JsonConvert.DeserializeObject<WorldClockData>(envelope.Data);
                        if (clock != null)
                            OnWorldClockSync?.Invoke(clock.WorldTick);
                        break;

                    case "PlayerJoined":
                        var joined = JsonConvert.DeserializeObject<PlayerEventData>(envelope.Data);
                        if (joined != null)
                            Log.Message($"[RimVerse] {joined.DisplayName} joined the server");
                        break;

                    case "PlayerLeft":
                        var left = JsonConvert.DeserializeObject<PlayerEventData>(rawData);
                        if (left != null)
                            Log.Message($"[RimVerse] {left.DisplayName} left the server");
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimVerse] Failed to parse server message: {ex.Message}");
            }
        }

        private class ServerMessage
        {
            public string Type { get; set; }
            public string Data { get; set; }
        }

        private class ChatMessageData
        {
            public string SenderName { get; set; }
            public string Channel { get; set; }
            public string Content { get; set; }
        }

        private class WorldClockData
        {
            public long WorldTick { get; set; }
        }

        private class PlayerEventData
        {
            public string PlayerId { get; set; }
            public string DisplayName { get; set; }
        }
    }

    public class AuthResponse
    {
        public string Token { get; set; }
        public string PlayerId { get; set; }
        public string DisplayName { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
