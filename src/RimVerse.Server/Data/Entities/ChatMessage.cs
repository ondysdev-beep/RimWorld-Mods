using System;

namespace RimVerse.Server.Data.Entities
{
    public class ChatMessage
    {
        public long Id { get; set; }
        public Guid WorldId { get; set; }
        public Guid SenderId { get; set; }
        public string Channel { get; set; } = "global";
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public World World { get; set; } = null!;
        public Player Sender { get; set; } = null!;
    }
}
