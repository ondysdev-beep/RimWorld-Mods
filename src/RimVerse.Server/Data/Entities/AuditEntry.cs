using System;

namespace RimVerse.Server.Data.Entities
{
    public class AuditEntry
    {
        public long Id { get; set; }
        public Guid? WorldId { get; set; }
        public Guid? ActorId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? DetailsJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public World? World { get; set; }
        public Player? Actor { get; set; }
    }
}
