using HarmonyLib;
using RimVerse.Client.Core;
using RimVerse.Client.Network;
using RimVerse.Client.UI;
using UnityEngine;
using Verse;

namespace RimVerse.Client.Patches
{
    [HarmonyPatch(typeof(UIRoot_Play), nameof(UIRoot_Play.Init))]
    public static class Patch_UIRoot_Init
    {
        static void Postfix()
        {
            var connMgr = new ConnectionManager();
            connMgr.OnChatMessageReceived += (sender, channel, content) =>
            {
                ChatWindow.AddMessage(sender, channel, content);
            };

            if (RimVerseMod.Settings.AutoConnect && !string.IsNullOrEmpty(RimVerseMod.Settings.SavedToken))
            {
                connMgr.Connect();
            }

            Log.Message("[RimVerse] ConnectionManager initialized");
        }
    }

    [HarmonyPatch(typeof(MainButtonsRoot), nameof(MainButtonsRoot.DoButtons))]
    public static class Patch_MainButtons
    {
        static void Postfix()
        {
            var buttonWidth = 80f;
            var buttonHeight = 35f;
            var x = Verse.UI.screenWidth - buttonWidth - 10f;
            var y = 10f;

            var buttonRect = new Rect(x, y, buttonWidth, buttonHeight);

            var connected = RimVerseMod.IsConnected;
            GUI.color = connected ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.3f, 0.3f);

            if (Widgets.ButtonText(buttonRect, "RimVerse"))
            {
                Find.WindowStack.Add(new MainWindow());
            }

            GUI.color = Color.white;

            if (connected)
            {
                var dotRect = new Rect(x - 12f, y + 12f, 8f, 8f);
                Widgets.DrawBoxSolid(dotRect, Color.green);
            }
        }
    }
}
