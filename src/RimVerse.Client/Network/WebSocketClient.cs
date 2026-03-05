using System;
using System.Threading;
using Verse;
using WebSocketSharp;

namespace RimVerse.Client.Network
{
    public class RimVerseWebSocket
    {
        private WebSocket _ws;
        private readonly string _url;
        private readonly string _authToken;
        private Timer _heartbeatTimer;
        private bool _intentionalClose;

        public bool IsConnected => _ws != null && _ws.IsAlive;

        public event Action OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<string, string> OnMessageReceived;

        public RimVerseWebSocket(string serverUrl, string authToken)
        {
            var wsUrl = serverUrl
                .Replace("https://", "wss://")
                .Replace("http://", "ws://")
                .TrimEnd('/');
            _url = wsUrl + "/hubs/game?access_token=" + Uri.EscapeDataString(authToken);
            _authToken = authToken;
        }

        public void Connect()
        {
            try
            {
                _intentionalClose = false;
                _ws = new WebSocket(_url);

                _ws.OnOpen += (sender, e) =>
                {
                    Log.Message("[RimVerse] WebSocket connected");
                    StartHeartbeat();
                    OnConnected?.Invoke();
                };

                _ws.OnMessage += (sender, e) =>
                {
                    HandleMessage(e.Data);
                };

                _ws.OnError += (sender, e) =>
                {
                    Log.Error($"[RimVerse] WebSocket error: {e.Message}");
                };

                _ws.OnClose += (sender, e) =>
                {
                    Log.Message($"[RimVerse] WebSocket closed: {e.Reason}");
                    StopHeartbeat();

                    if (!_intentionalClose)
                    {
                        OnDisconnected?.Invoke(e.Reason);
                        ScheduleReconnect();
                    }
                };

                _ws.ConnectAsync();
            }
            catch (Exception ex)
            {
                Log.Error($"[RimVerse] WebSocket connect failed: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            _intentionalClose = true;
            StopHeartbeat();
            if (_ws != null && _ws.IsAlive)
            {
                _ws.CloseAsync();
            }
        }

        public void Send(string messageType, string jsonPayload)
        {
            if (!IsConnected) return;

            var envelope = $"{{\"type\":\"{messageType}\",\"data\":{jsonPayload}}}";
            _ws.SendAsync(envelope, completed =>
            {
                if (!completed)
                    Log.Warning($"[RimVerse] Failed to send message: {messageType}");
            });
        }

        public void SendChat(string channel, string content)
        {
            var json = $"{{\"channel\":\"{channel}\",\"content\":\"{EscapeJson(content)}\"}}";
            Send("SendChatMessage", json);
        }

        private void HandleMessage(string rawData)
        {
            try
            {
                OnMessageReceived?.Invoke("raw", rawData);
            }
            catch (Exception ex)
            {
                Log.Error($"[RimVerse] Error handling message: {ex.Message}");
            }
        }

        private void StartHeartbeat()
        {
            _heartbeatTimer = new Timer(_ =>
            {
                if (IsConnected)
                {
                    Send("Heartbeat", "{}");
                }
            }, null, 15000, 15000);
        }

        private void StopHeartbeat()
        {
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;
        }

        private void ScheduleReconnect()
        {
            var timer = new Timer(_ =>
            {
                if (!_intentionalClose && !IsConnected)
                {
                    Log.Message("[RimVerse] Attempting reconnect...");
                    Connect();
                }
            }, null, 5000, Timeout.Infinite);
        }

        private static string EscapeJson(string s)
        {
            return s?.Replace("\\", "\\\\")
                     .Replace("\"", "\\\"")
                     .Replace("\n", "\\n")
                     .Replace("\r", "\\r")
                     .Replace("\t", "\\t") ?? "";
        }
    }
}
