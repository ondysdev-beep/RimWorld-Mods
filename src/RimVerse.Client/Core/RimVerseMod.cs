using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace RimVerse.Client.Core
{
    public class RimVerseMod : Mod
    {
        public static RimVerseMod Instance { get; private set; }
        public static RimVerseSettings Settings { get; private set; }
        public static Harmony HarmonyInstance { get; private set; }
        public static Guid LocalPlayerId { get; set; }
        public static string LocalPlayerName { get; set; }
        public static string AuthToken { get; set; }
        public static bool IsConnected { get; set; }

        public RimVerseMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<RimVerseSettings>();

            HarmonyInstance = new Harmony("rimverse.multiplayer");
            HarmonyInstance.PatchAll();

            Log.Message("[RimVerse] Mod initialized. Version 0.1.0");
        }

        public override string SettingsCategory() => "RimVerse";

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("Server URL:");
            Settings.ServerUrl = listing.TextEntry(Settings.ServerUrl);

            listing.Gap(12f);
            listing.Label("Username:");
            Settings.Username = listing.TextEntry(Settings.Username);

            listing.Gap(12f);
            listing.Label("Password:");
            Settings.Password = listing.TextEntry(Settings.Password);

            listing.Gap(12f);
            if (listing.ButtonText(IsConnected ? "Disconnect" : "Connect to Server"))
            {
                if (IsConnected)
                {
                    Network.ConnectionManager.Instance?.Disconnect();
                }
                else
                {
                    Network.ConnectionManager.Instance?.Connect();
                }
            }

            if (IsConnected)
            {
                listing.Gap(6f);
                listing.Label($"Connected as: {LocalPlayerName}");
            }

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }
    }
}
