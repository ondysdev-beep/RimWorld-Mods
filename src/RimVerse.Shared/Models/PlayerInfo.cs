using System;

namespace RimVerse.Shared.Models
{
    public class PlayerInfo
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Role { get; set; }
        public bool IsOnline { get; set; }
        public long LastSeenAt { get; set; }
    }
}
