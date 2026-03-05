using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RimVerse.Server.Data;
using RimVerse.Server.Data.Entities;
using RimVerse.Server.Models;
using RimVerse.Server.Services;

namespace RimVerse.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WorldController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly WorldClockService _worldClock;
        private readonly AuditService _audit;
        private readonly PlayerTracker _tracker;

        public WorldController(AppDbContext db, WorldClockService worldClock, AuditService audit, PlayerTracker tracker)
        {
            _db = db;
            _worldClock = worldClock;
            _audit = audit;
            _tracker = tracker;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<WorldInfoResponse>> GetWorld()
        {
            var world = await _db.Worlds.FirstOrDefaultAsync();
            if (world == null)
                return NotFound("No world configured.");

            var settlements = await _db.Settlements
                .Include(s => s.Owner)
                .Where(s => s.WorldId == world.Id)
                .Select(s => new SettlementResponse
                {
                    Id = s.Id.ToString(),
                    OwnerId = s.OwnerId.ToString(),
                    OwnerName = s.Owner.DisplayName,
                    TileId = s.TileId,
                    Name = s.Name,
                    Wealth = s.Wealth
                })
                .ToListAsync();

            return Ok(new WorldInfoResponse
            {
                Id = world.Id.ToString(),
                Name = world.Name,
                Seed = world.Seed,
                WorldTick = _worldClock.CurrentWorldTick,
                Storyteller = world.Storyteller,
                Difficulty = world.Difficulty,
                ModpackHash = world.ModpackHash,
                OnlinePlayers = _tracker.OnlineCount,
                Settlements = settlements
            });
        }

        [HttpPost("settlements")]
        public async Task<ActionResult<SettlementResponse>> CreateSettlement([FromBody] CreateSettlementRequest request)
        {
            var playerId = GetPlayerId();
            if (playerId == null) return Unauthorized();

            var world = await _db.Worlds.FirstOrDefaultAsync();
            if (world == null) return NotFound("No world configured.");

            var existingTile = await _db.Settlements
                .AnyAsync(s => s.WorldId == world.Id && s.TileId == request.TileId);
            if (existingTile)
                return Conflict("Tile is already occupied.");

            var minDistance = 3;
            var tooClose = await _db.Settlements
                .Where(s => s.WorldId == world.Id && s.OwnerId != playerId.Value)
                .AnyAsync(s => Math.Abs(s.TileId - request.TileId) < minDistance);
            if (tooClose)
                return Conflict("Too close to another player's settlement.");

            var settlement = new Settlement
            {
                Id = Guid.NewGuid(),
                WorldId = world.Id,
                OwnerId = playerId.Value,
                TileId = request.TileId,
                Name = request.Name,
                CreatedAt = DateTime.UtcNow
            };

            _db.Settlements.Add(settlement);
            await _db.SaveChangesAsync();

            var player = await _db.Players.FindAsync(playerId.Value);
            await _audit.LogAsync(world.Id, playerId, "settlement.create",
                $"{{\"tileId\":{request.TileId},\"name\":\"{request.Name}\"}}");

            return Ok(new SettlementResponse
            {
                Id = settlement.Id.ToString(),
                OwnerId = settlement.OwnerId.ToString(),
                OwnerName = player?.DisplayName ?? "Unknown",
                TileId = settlement.TileId,
                Name = settlement.Name,
                Wealth = settlement.Wealth
            });
        }

        [HttpGet("settlements")]
        [AllowAnonymous]
        public async Task<ActionResult> GetSettlements()
        {
            var world = await _db.Worlds.FirstOrDefaultAsync();
            if (world == null) return NotFound("No world configured.");

            var settlements = await _db.Settlements
                .Include(s => s.Owner)
                .Where(s => s.WorldId == world.Id)
                .Select(s => new SettlementResponse
                {
                    Id = s.Id.ToString(),
                    OwnerId = s.OwnerId.ToString(),
                    OwnerName = s.Owner.DisplayName,
                    TileId = s.TileId,
                    Name = s.Name,
                    Wealth = s.Wealth
                })
                .ToListAsync();

            return Ok(settlements);
        }

        [HttpDelete("settlements/{id}")]
        public async Task<ActionResult> DeleteSettlement(Guid id)
        {
            var playerId = GetPlayerId();
            if (playerId == null) return Unauthorized();

            var settlement = await _db.Settlements.FindAsync(id);
            if (settlement == null) return NotFound();
            if (settlement.OwnerId != playerId.Value) return Forbid();

            _db.Settlements.Remove(settlement);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(settlement.WorldId, playerId, "settlement.delete",
                $"{{\"tileId\":{settlement.TileId}}}");

            return NoContent();
        }

        [HttpGet("players")]
        public async Task<ActionResult> GetPlayers()
        {
            var players = await _db.Players
                .Where(p => !p.IsBanned)
                .Select(p => new PlayerResponse
                {
                    Id = p.Id.ToString(),
                    DisplayName = p.DisplayName,
                    Role = p.Role,
                    IsOnline = false
                })
                .ToListAsync();

            foreach (var p in players)
            {
                if (Guid.TryParse(p.Id, out var pid))
                    p.IsOnline = _tracker.IsOnline(pid);
            }

            return Ok(players);
        }

        private Guid? GetPlayerId()
        {
            var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst("sub")?.Value;
            return sub != null && Guid.TryParse(sub, out var id) ? id : null;
        }
    }
}
