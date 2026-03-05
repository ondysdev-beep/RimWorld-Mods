namespace RimVerse.Shared.Protocol
{
    public enum MessageType : ushort
    {
        // === SYSTEM ===
        Heartbeat           = 0x0001,
        HandshakeRequest    = 0x0002,
        HandshakeResponse   = 0x0003,
        Disconnect          = 0x0004,
        Error               = 0x0005,

        // === WORLD ===
        WorldClockSync      = 0x0100,
        SettlementUpdate    = 0x0101,
        PlayerJoined        = 0x0102,
        PlayerLeft          = 0x0103,

        // === CHAT ===
        ChatMessage         = 0x0200,
        ChatHistory         = 0x0201,

        // === TRADE / CONTRACTS ===
        ContractOffer       = 0x0300,
        ContractAccept      = 0x0301,
        ContractReject      = 0x0302,
        ContractComplete    = 0x0303,
        ContractCancel      = 0x0304,
        ParcelSent          = 0x0310,
        ParcelDelivered     = 0x0311,

        // === JOINT SESSION ===
        SessionRequest      = 0x0400,
        SessionInvite       = 0x0401,
        SessionAccept       = 0x0402,
        SessionReject       = 0x0403,
        SessionStart        = 0x0404,
        SessionTick         = 0x0405,
        SessionTickAck      = 0x0406,
        SessionHashCheck    = 0x0407,
        SessionDesync       = 0x0408,
        SessionEnd          = 0x0409,
        SessionDelta        = 0x040A,

        // === PERMISSIONS ===
        PermissionGrant     = 0x0500,
        PermissionRevoke    = 0x0501,
        BuilderBlueprintSubmit = 0x0510,
        BuilderBlueprintReview = 0x0511,

        // === MODPACK ===
        ModpackManifest     = 0x0600,
        ModpackHashMismatch = 0x0601,
        CompatStatusUpdate  = 0x0602,

        // === REPORT ===
        BugReportSubmit     = 0x0700
    }
}
