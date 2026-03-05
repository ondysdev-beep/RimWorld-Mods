namespace RimVerse.Shared.Models
{
    public class WorldState
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Seed { get; set; }
        public long WorldTick { get; set; }
        public string Storyteller { get; set; }
        public string Difficulty { get; set; }
        public string ModpackHash { get; set; }
        public int OnlinePlayerCount { get; set; }
        public int MaxPlayers { get; set; }
    }
}
