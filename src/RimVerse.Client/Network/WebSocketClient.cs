using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Verse;
using WebSocketSharp;

namespace RimVerse.Client.Network
{
    public class RimVerseWebSocket
    {
        private const char RecordSeparator = '\x1e';

        private WebSocket _ws;
        private readonly string _serverUrl;
        private readonly string _authToken;
        private Timer _heartbeatTimer;
        private bool _intentionalClose;
        private bool _handshakeCompleted;

        public bool IsConnected => _ws != null && _ws.IsAlive && _handshakeCompleted;

        public event Action OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<string, string> OnMessageReceived;

        public RimVerseWebSocket(string serverUrl, string authToken)
        {
            _serverUrl = serverUrl.TrimEnd('/');
            _authToken = authToken;
        }

        public void Connect()
        {
            try
            {
                _intentionalClose = false;
                _handshakeCompleted = false;

                var wsUrl = Negotiate();
                if (wsUrl == null) return;

                _ws = new WebSocket(wsUrl);

                if (wsUrl.StartsWith("wss://"))
                {
                    _ws.SslConfiguration.EnabledSslProtocols =
                        System.Security.Authentication.SslProtocols.Tls12;
                    _ws.SslConfiguration.ServerCertificateValidationCallback =
                        (sender2, certificate, chain, sslPolicyErrors) => true;
                }

                _ws.OnOpen += (sender, e) =>
                {
                    Log.Message("[RimVerse] WebSocket transport open, sending SignalR handshake...");
                    var handshake = JsonConvert.SerializeObject(new { protocol = "json", version = 1 }) + RecordSeparator;
                    _ws.Send(handshake);
                };

                _ws.OnMessage += (sender, e) =>
                {
                    HandleRawFrame(e.Data);
                };

                _ws.OnError += (sender, e) =>
                {
                    Log.Error($"[RimVerse] WebSocket error: {e.Message}");
                    if (e.Exception != null)
                        Log.Error($"[RimVerse] WebSocket exception: {e.Exception}");
                };

                _ws.OnClose += (sender, e) =>
                {
                    Log.Message($"[RimVerse] WebSocket closed: {e.Reason}");
                    _handshakeCompleted = false;
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
                if (!_intentionalClose)
                    ScheduleReconnect();
            }
        }

        private string Negotiate()
        {
            try
            {
                var negotiateUrl = _serverUrl + "/hubs/game/negotiate?negotiateVersion=1";
                var request = (HttpWebRequest)WebRequest.Create(negotiateUrl);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.ContentLength = 0;
                request.Timeout = 10000;
                request.Headers["Authorization"] = "Bearer " + _authToken;

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var json = reader.ReadToEnd();
                    var negotiateResponse = JObject.Parse(json);

                    var connectionToken = negotiateResponse["connectionToken"]?.ToString();
                    if (string.IsNullOrEmpty(connectionToken))
                    {
                        Log.Error("[RimVerse] SignalR negotiate: no connectionToken in response");
                        return null;
                    }

                    var wsUrl = _serverUrl
                        .Replace("https://", "wss://")
                        .Replace("http://", "ws://");
                    wsUrl += "/hubs/game?id=" + Uri.EscapeDataString(connectionToken)
                          + "&access_token=" + Uri.EscapeDataString(_authToken);

                    Log.Message("[RimVerse] SignalR negotiate successful");
                    return wsUrl;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimVerse] SignalR negotiate failed: {ex.Message}");
                return null;
            }
        }

        public void Disconnect()
        {
            _intentionalClose = true;
            _handshakeCompleted = false;
            StopHeartbeat();
            if (_ws != null && _ws.IsAlive)
            {
                _ws.CloseAsync();
            }
        }

        public void Invoke(string method, params object[] args)
        {
            if (!IsConnected) return;

            var msg = new
            {
                type = 1,
                target = method,
                arguments = args
            };
            var json = JsonConvert.SerializeObject(msg) + RecordSeparator;
            _ws.SendAsync(json, completed =>
            {
                if (!completed)
                    Log.Warning($"[RimVerse] Failed to invoke: {method}");
            });
        }

        public void SendChat(string channel, string content)
        {
            Invoke("SendChatMessage", channel, content);
        }

        private void HandleRawFrame(string rawData)
        {
            if (string.IsNullOrEmpty(rawData)) return;

            var messages = rawData.Split(new[] { RecordSeparator }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var message in messages)
            {
                try
                {
                    var obj = JObject.Parse(message);
                    var msgType = obj["type"]?.Value<int>() ?? 0;

                    if (!_handshakeCompleted)
                    {
                        if (obj["error"] != null)
                        {
                            Log.Error($"[RimVerse] SignalR handshake error: {obj["error"]}");
                            return;
                        }
                        _handshakeCompleted = true;
                        Log.Message("[RimVerse] SignalR handshake completed");
                        StartHeartbeat();
                        OnConnected?.Invoke();
                        return;
                    }

                    switch (msgType)
                    {
                        case 1: // Invocation
                            var target = obj["target"]?.ToString();
                            var args = obj["arguments"]?.ToString();
                            if (!string.IsNullOrEmpty(target))
                                OnMessageReceived?.Invoke(target, args ?? "[]");
                            break;

                        case 6: // Ping
                            var pong = JsonConvert.SerializeObject(new { type = 6 }) + RecordSeparator;
                            _ws.Send(pong);
                            break;

                        case 7: // Close
                            var reason = obj["error"]?.ToString() ?? "Server closed connection";
                            Log.Warning($"[RimVerse] Server closing: {reason}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[RimVerse] Error parsing SignalR message: {ex.Message}");
                }
            }
        }

        private void StartHeartbeat()
        {
            _heartbeatTimer = new Timer(_ =>
            {
                if (IsConnected)
                {
                    var ping = JsonConvert.SerializeObject(new { type = 6 }) + RecordSeparator;
                    _ws.Send(ping);
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
    }
}
