using Verse;

namespace RimVerse.Client.Core
{
    public class RimVerseSettings : ModSettings
    {
        public string ServerUrl = "https://rimverse-server.fly.dev";
        public string Username = "";
        public string Password = "";
        public string SavedToken = "";
        public string SavedPlayerId = "";
        public bool AutoConnect = false;
        public bool ShowChatOverlay = true;
        public float ChatOpacity = 0.8f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref ServerUrl, "serverUrl", "https://rimverse-server.fly.dev");
            Scribe_Values.Look(ref Username, "username", "");
            Scribe_Values.Look(ref Password, "password", "");
            Scribe_Values.Look(ref SavedToken, "savedToken", "");
            Scribe_Values.Look(ref SavedPlayerId, "savedPlayerId", "");
            Scribe_Values.Look(ref AutoConnect, "autoConnect", false);
            Scribe_Values.Look(ref ShowChatOverlay, "showChatOverlay", true);
            Scribe_Values.Look(ref ChatOpacity, "chatOpacity", 0.8f);
            base.ExposeData();
        }
    }
}
