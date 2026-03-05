using System;
using System.Collections.Generic;

namespace RimVerse.Server.Data.Entities
{
    public class Player
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? SteamId { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "player";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
        public bool IsBanned { get; set; }
        public string? BanReason { get; set; }

        public ICollection<Settlement> Settlements { get; set; } = new List<Settlement>();
    }
}
