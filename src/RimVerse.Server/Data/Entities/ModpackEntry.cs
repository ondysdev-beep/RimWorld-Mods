using System;

namespace RimVerse.Server.Data.Entities
{
    public class ModpackEntry
    {
        public Guid Id { get; set; }
        public Guid WorldId { get; set; }
        public string PackageId { get; set; } = string.Empty;
        public string? ModName { get; set; }
        public string? Version { get; set; }
        public string CompatStatus { get; set; } = "unknown";
        public string? Notes { get; set; }

        public World World { get; set; } = null!;
    }
}
