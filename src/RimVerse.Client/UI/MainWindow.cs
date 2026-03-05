using System;
using System.Collections.Generic;
using RimVerse.Client.Core;
using RimVerse.Client.Network;
using UnityEngine;
using Verse;

namespace RimVerse.Client.UI
{
    public class MainWindow : Window
    {
        private enum Tab { Overview, Players, Chat, Trade, Settings }

        private Tab _currentTab = Tab.Overview;
        private Vector2 _scrollPos;

        public override Vector2 InitialSize => new Vector2(650f, 500f);

        public MainWindow()
        {
            doCloseButton = true;
            draggable = true;
            resizeable = true;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var headerRect = new Rect(inRect.x, inRect.y, inRect.width, 30f);
            DrawHeader(headerRect);

            var tabRect = new Rect(inRect.x, headerRect.yMax + 4f, inRect.width, 30f);
            DrawTabs(tabRect);

            var contentRect = new Rect(inRect.x, tabRect.yMax + 4f, inRect.width, inRect.height - 80f);
            switch (_currentTab)
            {
                case Tab.Overview:
                    DrawOverview(contentRect);
                    break;
                case Tab.Players:
                    DrawPlayers(contentRect);
                    break;
                case Tab.Chat:
                    ChatWindow.DrawChatPanel(contentRect);
                    break;
                case Tab.Trade:
                    DrawTrade(contentRect);
                    break;
                case Tab.Settings:
                    DrawSettings(contentRect);
                    break;
            }
        }

        private void DrawHeader(Rect rect)
        {
            Text.Font = GameFont.Medium;
            var connected = RimVerseMod.IsConnected;
            var statusColor = connected ? Color.green : Color.red;
            var statusText = connected ? "Connected" : "Disconnected";

            Widgets.Label(rect, "RimVerse");

            var statusRect = new Rect(rect.xMax - 200f, rect.y, 200f, rect.height);
            GUI.color = statusColor;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(statusRect, statusText);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawTabs(Rect rect)
        {
            var tabs = new List<TabRecord>
            {
                new TabRecord("Overview", () => _currentTab = Tab.Overview, _currentTab == Tab.Overview),
                new TabRecord("Players", () => _currentTab = Tab.Players, _currentTab == Tab.Players),
                new TabRecord("Chat", () => _currentTab = Tab.Chat, _currentTab == Tab.Chat),
                new TabRecord("Trade", () => _currentTab = Tab.Trade, _currentTab == Tab.Trade),
                new TabRecord("Settings", () => _currentTab = Tab.Settings, _currentTab == Tab.Settings),
            };
            TabDrawer.DrawTabs(rect, tabs);
        }

        private void DrawOverview(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);

            if (!RimVerseMod.IsConnected)
            {
                listing.Label("Not connected to any server.");
                listing.Gap(8f);
                if (listing.ButtonText("Connect"))
                {
                    ConnectionManager.Instance?.Connect();
                }
            }
            else
            {
                listing.Label($"Player: {RimVerseMod.LocalPlayerName}");
                listing.Label($"Server: {RimVerseMod.Settings.ServerUrl}");
                listing.Gap(12f);
                listing.Label("--- Active Contracts ---");
                listing.Label("(No active contracts)");
                listing.Gap(12f);
                listing.Label("--- Incoming Parcels ---");
                listing.Label("(No incoming parcels)");
            }

            listing.End();
        }

        private void DrawPlayers(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);

            if (!RimVerseMod.IsConnected)
            {
                listing.Label("Connect to a server to see players.");
            }
            else
            {
                listing.Label("Online Players:");
                listing.Gap(4f);
                listing.Label("(Player list loads from server)");
            }

            listing.End();
        }

        private void DrawTrade(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);

            if (!RimVerseMod.IsConnected)
            {
                listing.Label("Connect to a server to trade.");
            }
            else
            {
                listing.Label("Trade & Contracts");
                listing.Gap(8f);
                if (listing.ButtonText("Create New Trade Offer"))
                {
                    Log.Message("[RimVerse] Trade creation UI - TODO");
                }
                listing.Gap(12f);
                listing.Label("--- My Contracts ---");
                listing.Label("(Loading...)");
            }

            listing.End();
        }

        private void DrawSettings(Rect rect)
        {
            RimVerseMod.Instance.DoSettingsWindowContents(rect);
        }
    }
}
