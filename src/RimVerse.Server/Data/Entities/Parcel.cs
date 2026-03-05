using System;

namespace RimVerse.Server.Data.Entities
{
    public class Parcel
    {
        public Guid Id { get; set; }
        public Guid? ContractId { get; set; }
        public Guid WorldId { get; set; }
        public Guid SenderId { get; set; }
        public Guid ReceiverId { get; set; }
        public string ItemsJson { get; set; } = "[]";
        public string? PawnsJson { get; set; }
        public string Status { get; set; } = "in_transit";
        public long SendWorldTick { get; set; }
        public long EtaWorldTick { get; set; }
        public DateTime? DeliveredAt { get; set; }

        public Contract? Contract { get; set; }
        public World World { get; set; } = null!;
        public Player Sender { get; set; } = null!;
        public Player Receiver { get; set; } = null!;
    }
}
