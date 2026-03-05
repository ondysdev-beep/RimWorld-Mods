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
    public class TradeController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly WorldClockService _worldClock;
        private readonly AuditService _audit;

        public TradeController(AppDbContext db, WorldClockService worldClock, AuditService audit)
        {
            _db = db;
            _worldClock = worldClock;
            _audit = audit;
        }

        [HttpPost("contracts")]
        public async Task<ActionResult<ContractResponse>> CreateContract([FromBody] CreateContractRequest request)
        {
            var playerId = GetPlayerId();
            if (playerId == null) return Unauthorized();

            if (!Guid.TryParse(request.TargetPlayerId, out var targetId))
                return BadRequest("Invalid target player ID.");

            if (targetId == playerId.Value)
                return BadRequest("Cannot create a contract with yourself.");

            var targetPlayer = await _db.Players.FindAsync(targetId);
            if (targetPlayer == null) return NotFound("Target player not found.");

            var world = await _db.Worlds.FirstOrDefaultAsync();
            if (world == null) return NotFound("No world configured.");

            var contract = new Contract
            {
                Id = Guid.NewGuid(),
                WorldId = world.Id,
                Type = request.Type,
                Status = "pending",
                InitiatorId = playerId.Value,
                TargetId = targetId,
                OfferItemsJson = request.OfferItemsJson,
                RequestItemsJson = request.RequestItemsJson,
                ScheduledWorldTick = _worldClock.CurrentWorldTick,
                ExpiresWorldTick = _worldClock.CurrentWorldTick + request.ExpiresInWorldTicks,
                CreatedAt = DateTime.UtcNow
            };

            _db.Contracts.Add(contract);
            await _db.SaveChangesAsync();

            var initiator = await _db.Players.FindAsync(playerId.Value);
            await _audit.LogAsync(world.Id, playerId, "contract.create",
                $"{{\"contractId\":\"{contract.Id}\",\"type\":\"{request.Type}\",\"target\":\"{targetId}\"}}");

            return Ok(MapContractResponse(contract, initiator!.DisplayName, targetPlayer.DisplayName));
        }

        [HttpGet("contracts")]
        public async Task<ActionResult> GetMyContracts()
        {
            var playerId = GetPlayerId();
            if (playerId == null) return Unauthorized();

            var contracts = await _db.Contracts
                .Include(c => c.Initiator)
                .Include(c => c.Target)
                .Where(c => (c.InitiatorId == playerId.Value || c.TargetId == playerId.Value)
                            && c.Status != "completed" && c.Status != "cancelled")
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var result = contracts.Select(c =>
                MapContractResponse(c, c.Initiator.DisplayName, c.Target.DisplayName));

            return Ok(result);
        }

        [HttpPost("contracts/{id}/accept")]
        public async Task<ActionResult> AcceptContract(Guid id)
        {
            var playerId = GetPlayerId();
            if (playerId == null) return Unauthorized();

            var contract = await _db.Contracts.FindAsync(id);
            if (contract == null) return NotFound();
            if (contract.TargetId != playerId.Value) return Forbid();
            if (contract.Status != "pending") return BadRequest("Contract is not pending.");

            if (_worldClock.CurrentWorldTick > contract.ExpiresWorldTick)
            {
                contract.Status = "cancelled";
                await _db.SaveChangesAsync();
                return BadRequest("Contract has expired.");
            }

            contract.Status = "accepted";
            contract.EscrowLocked = true;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(contract.WorldId, playerId, "contract.accept",
                $"{{\"contractId\":\"{id}\"}}");

            return Ok(new { message = "Contract accepted. Escrow locked." });
        }

        [HttpPost("contracts/{id}/reject")]
        public async Task<ActionResult> RejectContract(Guid id)
        {
            var playerId = GetPlayerId();
            if (playerId == null) return Unauthorized();

            var contract = await _db.Contracts.FindAsync(id);
            if (contract == null) return NotFound();
            if (contract.TargetId != playerId.Value && contract.InitiatorId != playerId.Value) return Forbid();
            if (contract.Status != "pending") return BadRequest("Contract is not pending.");

            contract.Status = "cancelled";
            contract.ResolvedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(contract.WorldId, playerId, "contract.reject",
                $"{{\"contractId\":\"{id}\"}}");

            return Ok(new { message = "Contract rejected." });
        }

        [HttpPost("contracts/{id}/complete")]
        public async Task<ActionResult> CompleteContract(Guid id)
        {
            var playerId = GetPlayerId();
            if (playerId == null) return Unauthorized();

            var contract = await _db.Contracts.FindAsync(id);
            if (contract == null) return NotFound();
            if (contract.InitiatorId != playerId.Value && contract.TargetId != playerId.Value) return Forbid();
            if (contract.Status != "accepted") return BadRequest("Contract must be accepted first.");

            contract.Status = "completed";
            contract.EscrowLocked = false;
            contract.ResolvedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(contract.WorldId, playerId, "contract.complete",
                $"{{\"contractId\":\"{id}\"}}");

            return Ok(new { message = "Contract completed." });
        }

        private static ContractResponse MapContractResponse(Contract c, string initiatorName, string targetName)
        {
            return new ContractResponse
            {
                Id = c.Id.ToString(),
                Type = c.Type,
                Status = c.Status,
                InitiatorId = c.InitiatorId.ToString(),
                InitiatorName = initiatorName,
                TargetId = c.TargetId.ToString(),
                TargetName = targetName,
                OfferItemsJson = c.OfferItemsJson,
                RequestItemsJson = c.RequestItemsJson,
                ExpiresWorldTick = c.ExpiresWorldTick,
                EscrowLocked = c.EscrowLocked
            };
        }

        private Guid? GetPlayerId()
        {
            var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst("sub")?.Value;
            return sub != null && Guid.TryParse(sub, out var id) ? id : null;
        }
    }
}
