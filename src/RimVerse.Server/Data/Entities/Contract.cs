using System;

namespace RimVerse.Server.Data.Entities
{
    public class Contract
    {
        public Guid Id { get; set; }
        public Guid WorldId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = "pending";
        public Guid InitiatorId { get; set; }
        public Guid TargetId { get; set; }
        public string? OfferItemsJson { get; set; }
        public string? RequestItemsJson { get; set; }
        public long ScheduledWorldTick { get; set; }
        public long ExpiresWorldTick { get; set; }
        public bool EscrowLocked { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }

        public World World { get; set; } = null!;
        public Player Initiator { get; set; } = null!;
        public Player Target { get; set; } = null!;
    }
}
