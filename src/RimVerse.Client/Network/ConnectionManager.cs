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

        private void HandleServerMessage(string target, string argsJson)
        {
            try
            {
                var args = Newtonsoft.Json.Linq.JArray.Parse(argsJson);

                switch (target)
                {
                    case "ReceiveChatMessage":
                        if (args.Count >= 3)
                            OnChatMessageReceived?.Invoke(args[0].ToString(), args[1].ToString(), args[2].ToString());
                        break;

                    case "WorldClockSync":
                        if (args.Count >= 1)
                            OnWorldClockSync?.Invoke((long)args[0]);
                        break;

                    case "PlayerJoined":
                        if (args.Count >= 2)
                            Log.Message($"[RimVerse] {args[1]} joined the server");
                        break;

                    case "PlayerLeft":
                        if (args.Count >= 2)
                            Log.Message($"[RimVerse] {args[1]} left the server");
                        break;

                    default:
                        Log.Message($"[RimVerse] Unhandled hub method: {target}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimVerse] Failed to parse server message '{target}': {ex.Message}");
            }
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
