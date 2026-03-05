using System;

namespace RimVerse.Server.Data.Entities
{
    public class World
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Seed { get; set; } = string.Empty;
        public long WorldTick { get; set; }
        public string Storyteller { get; set; } = "Cassandra";
        public string Difficulty { get; set; } = "Rough";
        public string ModpackHash { get; set; } = string.Empty;
        public string? ConfigJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
