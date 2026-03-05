using System;
using System.Collections.Generic;

namespace RimVerse.Server.Models
{
    // === AUTH ===
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? SteamId { get; set; }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string PlayerId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    // === WORLD ===
    public class CreateSettlementRequest
    {
        public int TileId { get; set; }
        public string? Name { get; set; }
    }

    public class SettlementResponse
    {
        public string Id { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public int TileId { get; set; }
        public string? Name { get; set; }
        public float Wealth { get; set; }
    }

    public class WorldInfoResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Seed { get; set; } = string.Empty;
        public long WorldTick { get; set; }
        public string Storyteller { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string ModpackHash { get; set; } = string.Empty;
        public int OnlinePlayers { get; set; }
        public List<SettlementResponse> Settlements { get; set; } = new();
    }

    // === TRADE ===
    public class CreateContractRequest
    {
        public string TargetPlayerId { get; set; } = string.Empty;
        public string Type { get; set; } = "trade";
        public string? OfferItemsJson { get; set; }
        public string? RequestItemsJson { get; set; }
        public long ExpiresInWorldTicks { get; set; } = 60000;
    }

    public class ContractResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string InitiatorId { get; set; } = string.Empty;
        public string InitiatorName { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public string? OfferItemsJson { get; set; }
        public string? RequestItemsJson { get; set; }
        public long ExpiresWorldTick { get; set; }
        public bool EscrowLocked { get; set; }
    }

    // === PLAYERS ===
    public class PlayerResponse
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
    }
}
