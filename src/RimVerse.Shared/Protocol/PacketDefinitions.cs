using System;
using MessagePack;

namespace RimVerse.Shared.Protocol
{
    [MessagePackObject]
    public class HandshakeRequest
    {
        [Key(0)] public string ProtocolVersion { get; set; }
        [Key(1)] public string GameVersion { get; set; }
        [Key(2)] public string AuthToken { get; set; }
        [Key(3)] public string ModpackHash { get; set; }
        [Key(4)] public string[] ModList { get; set; }
    }

    [MessagePackObject]
    public class HandshakeResponse
    {
        [Key(0)] public bool Success { get; set; }
        [Key(1)] public string PlayerId { get; set; }
        [Key(2)] public string WorldName { get; set; }
        [Key(3)] public long WorldTick { get; set; }
        [Key(4)] public string ErrorMessage { get; set; }
    }

    [MessagePackObject]
    public class WorldClockSyncMessage
    {
        [Key(0)] public long WorldTick { get; set; }
        [Key(1)] public long ServerTimestampMs { get; set; }
    }

    [MessagePackObject]
    public class ChatMessagePacket
    {
        [Key(0)] public string SenderId { get; set; }
        [Key(1)] public string SenderName { get; set; }
        [Key(2)] public string Channel { get; set; }
        [Key(3)] public string Content { get; set; }
        [Key(4)] public long Timestamp { get; set; }
    }

    [MessagePackObject]
    public class SessionTickMessage
    {
        [Key(0)] public string SessionId { get; set; }
        [Key(1)] public long TickNumber { get; set; }
        [Key(2)] public byte[] PlayerInputs { get; set; }
        [Key(3)] public uint StateHash { get; set; }
    }

    [MessagePackObject]
    public class SessionRequestMessage
    {
        [Key(0)] public string TargetPlayerId { get; set; }
        [Key(1)] public string SessionType { get; set; }
    }

    [MessagePackObject]
    public class SessionStartMessage
    {
        [Key(0)] public string SessionId { get; set; }
        [Key(1)] public string[] ParticipantIds { get; set; }
        [Key(2)] public long RngSeed { get; set; }
        [Key(3)] public string SessionType { get; set; }
        [Key(4)] public byte[] MapData { get; set; }
    }

    [MessagePackObject]
    public class ContractOfferMessage
    {
        [Key(0)] public string ContractId { get; set; }
        [Key(1)] public string TargetPlayerId { get; set; }
        [Key(2)] public TradeItem[] OfferedItems { get; set; }
        [Key(3)] public TradeItem[] RequestedItems { get; set; }
        [Key(4)] public long ExpiresAtWorldTick { get; set; }
    }

    [MessagePackObject]
    public class TradeItem
    {
        [Key(0)] public string DefName { get; set; }
        [Key(1)] public int Quantity { get; set; }
        [Key(2)] public int Quality { get; set; }
        [Key(3)] public string StuffDefName { get; set; }
    }

    [MessagePackObject]
    public class SettlementUpdateMessage
    {
        [Key(0)] public string SettlementId { get; set; }
        [Key(1)] public string OwnerName { get; set; }
        [Key(2)] public int TileId { get; set; }
        [Key(3)] public string Name { get; set; }
        [Key(4)] public float Wealth { get; set; }
    }

    [MessagePackObject]
    public class PlayerJoinedMessage
    {
        [Key(0)] public string PlayerId { get; set; }
        [Key(1)] public string DisplayName { get; set; }
    }

    [MessagePackObject]
    public class ErrorMessage
    {
        [Key(0)] public ushort Code { get; set; }
        [Key(1)] public string Message { get; set; }
    }

    [MessagePackObject]
    public class ParcelSentMessage
    {
        [Key(0)] public string ParcelId { get; set; }
        [Key(1)] public string ReceiverId { get; set; }
        [Key(2)] public TradeItem[] Items { get; set; }
        [Key(3)] public long EtaWorldTick { get; set; }
    }

    [MessagePackObject]
    public class ParcelDeliveredMessage
    {
        [Key(0)] public string ParcelId { get; set; }
        [Key(1)] public TradeItem[] Items { get; set; }
        [Key(2)] public string SenderName { get; set; }
    }
}
