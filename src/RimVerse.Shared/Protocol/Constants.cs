namespace RimVerse.Shared.Protocol
{
    public static class Constants
    {
        public const string ProtocolVersion = "1.0.0";
        public const int MaxPlayerNameLength = 64;
        public const int MaxChatMessageLength = 500;
        public const int MaxModListSize = 500;
        public const int HeartbeatIntervalMs = 15000;
        public const int HeartbeatTimeoutMs = 45000;
        public const int SessionTickTimeoutMs = 5000;
        public const int DesyncCheckIntervalTicks = 60;
        public const int MaxSessionDurationTicks = 120000;
        public const int DefaultClaimRadius = 3;
        public const int WorldClockSyncIntervalMs = 60000;
    }
}
