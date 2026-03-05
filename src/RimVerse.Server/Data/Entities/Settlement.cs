using System;

namespace RimVerse.Server.Data.Entities
{
    public class Settlement
    {
        public Guid Id { get; set; }
        public Guid WorldId { get; set; }
        public Guid OwnerId { get; set; }
        public int TileId { get; set; }
        public string? Name { get; set; }
        public long LocalTick { get; set; }
        public float Wealth { get; set; }
        public byte[]? SnapshotData { get; set; }
        public DateTime? SnapshotAt { get; set; }
        public int ClaimRadius { get; set; } = 3;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public World World { get; set; } = null!;
        public Player Owner { get; set; } = null!;
    }
}
