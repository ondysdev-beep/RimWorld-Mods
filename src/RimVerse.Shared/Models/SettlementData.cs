namespace RimVerse.Shared.Models
{
    public class SettlementData
    {
        public string Id { get; set; }
        public string OwnerId { get; set; }
        public string OwnerName { get; set; }
        public int TileId { get; set; }
        public string Name { get; set; }
        public long LocalTick { get; set; }
        public float Wealth { get; set; }
        public int ClaimRadius { get; set; }
    }
}
