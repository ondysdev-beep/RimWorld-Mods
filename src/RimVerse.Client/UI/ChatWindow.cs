using System;
using System.Collections.Generic;
using RimVerse.Client.Core;
using RimVerse.Client.Network;
using UnityEngine;
using Verse;

namespace RimVerse.Client.UI
{
    public static class ChatWindow
    {
        private static readonly List<ChatEntry> _messages = new List<ChatEntry>();
        private static string _inputText = "";
        private static Vector2 _scrollPos;
        private static string _currentChannel = "global";
        private const int MaxMessages = 200;

        public static void AddMessage(string sender, string channel, string content)
        {
            _messages.Add(new ChatEntry
            {
                Sender = sender,
                Channel = channel,
                Content = content,
                Timestamp = DateTime.Now
            });

            if (_messages.Count > MaxMessages)
                _messages.RemoveAt(0);

            _scrollPos.y = float.MaxValue;
        }

        public static void DrawChatPanel(Rect rect)
        {
            var channelRect = new Rect(rect.x, rect.y, rect.width, 24f);
            DrawChannelSelector(channelRect);

            var messagesRect = new Rect(rect.x, channelRect.yMax + 4f, rect.width, rect.height - 64f);
            DrawMessages(messagesRect);

            var inputRect = new Rect(rect.x, messagesRect.yMax + 4f, rect.width - 70f, 28f);
            var sendRect = new Rect(inputRect.xMax + 4f, inputRect.y, 64f, 28f);
            DrawInput(inputRect, sendRect);
        }

        private static void DrawChannelSelector(Rect rect)
        {
            var globalRect = new Rect(rect.x, rect.y, 80f, rect.height);
            var tradeRect = new Rect(globalRect.xMax + 4f, rect.y, 80f, rect.height);

            if (Widgets.ButtonText(globalRect, "Global", active: _currentChannel != "global"))
                _currentChannel = "global";
            if (Widgets.ButtonText(tradeRect, "Trade", active: _currentChannel != "trade"))
                _currentChannel = "trade";
        }

        private static void DrawMessages(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.8f));

            var filtered = _messages.FindAll(m => m.Channel == _currentChannel || m.Channel == "system");
            var innerHeight = filtered.Count * 22f;
            var viewRect = new Rect(0f, 0f, rect.width - 16f, Math.Max(innerHeight, rect.height));

            Widgets.BeginScrollView(rect, ref _scrollPos, viewRect);

            var y = 0f;
            foreach (var msg in filtered)
            {
                var lineRect = new Rect(4f, y, viewRect.width - 8f, 20f);
                var timeStr = msg.Timestamp.ToString("HH:mm");

                GUI.color = msg.Channel == "system" ? Color.yellow : Color.white;
                var senderColor = GetSenderColor(msg.Sender);

                Text.Font = GameFont.Tiny;
                Widgets.Label(lineRect, $"[{timeStr}] <color=#{ColorUtility.ToHtmlStringRGB(senderColor)}>{msg.Sender}</color>: {msg.Content}");
                Text.Font = GameFont.Small;
                GUI.color = Color.white;

                y += 22f;
            }

            Widgets.EndScrollView();
        }

        private static void DrawInput(Rect inputRect, Rect sendRect)
        {
            if (!RimVerseMod.IsConnected)
            {
                GUI.enabled = false;
                _inputText = "Not connected...";
            }

            _inputText = Widgets.TextField(inputRect, _inputText);

            if (Widgets.ButtonText(sendRect, "Send") || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return))
            {
                if (!string.IsNullOrWhiteSpace(_inputText) && RimVerseMod.IsConnected)
                {
                    ConnectionManager.Instance?.SendChat(_currentChannel, _inputText.Trim());
                    AddMessage(RimVerseMod.LocalPlayerName ?? "Me", _currentChannel, _inputText.Trim());
                    _inputText = "";
                }
            }

            GUI.enabled = true;
        }

        private static Color GetSenderColor(string name)
        {
            if (string.IsNullOrEmpty(name)) return Color.white;
            var hash = name.GetHashCode();
            var r = ((hash & 0xFF0000) >> 16) / 255f * 0.5f + 0.5f;
            var g = ((hash & 0x00FF00) >> 8) / 255f * 0.5f + 0.5f;
            var b = (hash & 0x0000FF) / 255f * 0.5f + 0.5f;
            return new Color(r, g, b);
        }

        private class ChatEntry
        {
            public string Sender;
            public string Channel;
            public string Content;
            public DateTime Timestamp;
        }
    }
}
