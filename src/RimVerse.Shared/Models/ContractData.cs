using RimVerse.Shared.Protocol;

namespace RimVerse.Shared.Models
{
    public class ContractData
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string InitiatorId { get; set; }
        public string InitiatorName { get; set; }
        public string TargetId { get; set; }
        public string TargetName { get; set; }
        public TradeItem[] OfferItems { get; set; }
        public TradeItem[] RequestItems { get; set; }
        public long ScheduledWorldTick { get; set; }
        public long ExpiresWorldTick { get; set; }
        public bool EscrowLocked { get; set; }
    }
}
