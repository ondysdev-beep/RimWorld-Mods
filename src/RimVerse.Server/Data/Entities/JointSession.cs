using System;

namespace RimVerse.Server.Data.Entities
{
    public class JointSession
    {
        public Guid Id { get; set; }
        public Guid WorldId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = "pending";
        public Guid HostId { get; set; }
        public string ModpackHash { get; set; } = string.Empty;
        public long RngSeed { get; set; }
        public long MaxTick { get; set; } = 60000;
        public long CurrentTick { get; set; }
        public string ParticipantsJson { get; set; } = "[]";
        public string? ReplayDataUrl { get; set; }
        public string? DeltaJson { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public World World { get; set; } = null!;
        public Player Host { get; set; } = null!;
    }
}
