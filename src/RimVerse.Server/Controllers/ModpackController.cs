using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RimVerse.Server.Data;
using RimVerse.Server.Data.Entities;

namespace RimVerse.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ModpackController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ModpackController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("manifest")]
        [AllowAnonymous]
        public async Task<ActionResult> GetManifest()
        {
            var world = await _db.Worlds.FirstOrDefaultAsync();
            if (world == null) return NotFound("No world configured.");

            var mods = await _db.ModpackEntries
                .Where(m => m.WorldId == world.Id)
                .OrderBy(m => m.PackageId)
                .Select(m => new
                {
                    m.PackageId,
                    m.ModName,
                    m.Version,
                    m.CompatStatus,
                    m.Notes
                })
                .ToListAsync();

            return Ok(new
            {
                WorldName = world.Name,
                ModpackHash = world.ModpackHash,
                RequiredMods = mods.Where(m => m.CompatStatus != "banned").ToList(),
                BannedMods = mods.Where(m => m.CompatStatus == "banned").Select(m => m.PackageId).ToList()
            });
        }

        [HttpPost("validate")]
        public async Task<ActionResult> ValidateModpack([FromBody] ModpackValidateRequest request)
        {
            var world = await _db.Worlds.FirstOrDefaultAsync();
            if (world == null) return NotFound("No world configured.");

            if (world.ModpackHash == "none" || world.ModpackHash == request.ClientHash)
            {
                return Ok(new { Valid = true, Message = "Modpack hash matches." });
            }

            var serverMods = await _db.ModpackEntries
                .Where(m => m.WorldId == world.Id && m.CompatStatus != "banned")
                .Select(m => m.PackageId.ToLower())
                .ToListAsync();

            var clientMods = request.ModList?.Select(m => m.ToLower()).ToHashSet()
                             ?? new System.Collections.Generic.HashSet<string>();

            var missing = serverMods.Where(m => !clientMods.Contains(m)).ToList();
            var extra = clientMods.Where(m => !serverMods.Contains(m)).ToList();

            var bannedMods = await _db.ModpackEntries
                .Where(m => m.WorldId == world.Id && m.CompatStatus == "banned")
                .Select(m => m.PackageId.ToLower())
                .ToListAsync();

            var clientHasBanned = clientMods.Intersect(bannedMods).ToList();

            return Ok(new
            {
                Valid = missing.Count == 0 && clientHasBanned.Count == 0,
                MissingMods = missing,
                ExtraMods = extra,
                BannedModsDetected = clientHasBanned,
                ServerHash = world.ModpackHash,
                ClientHash = request.ClientHash
            });
        }

        [HttpPost("entries")]
        public async Task<ActionResult> AddModEntry([FromBody] AddModEntryRequest request)
        {
            var playerId = GetPlayerId();
            if (playerId == null) return Unauthorized();

            var player = await _db.Players.FindAsync(playerId.Value);
            if (player == null || player.Role != "admin")
                return Forbid("Only admins can modify the modpack.");

            var world = await _db.Worlds.FirstOrDefaultAsync();
            if (world == null) return NotFound("No world configured.");

            var existing = await _db.ModpackEntries
                .FirstOrDefaultAsync(m => m.WorldId == world.Id && m.PackageId == request.PackageId);

            if (existing != null)
            {
                existing.ModName = request.ModName ?? existing.ModName;
                existing.Version = request.Version ?? existing.Version;
                existing.CompatStatus = request.CompatStatus ?? existing.CompatStatus;
                existing.Notes = request.Notes ?? existing.Notes;
            }
            else
            {
                _db.ModpackEntries.Add(new ModpackEntry
                {
                    Id = Guid.NewGuid(),
                    WorldId = world.Id,
                    PackageId = request.PackageId,
                    ModName = request.ModName,
                    Version = request.Version,
                    CompatStatus = request.CompatStatus ?? "unknown",
                    Notes = request.Notes
                });
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "Mod entry saved." });
        }

        private Guid? GetPlayerId()
        {
            var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst("sub")?.Value;
            return sub != null && Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public class ModpackValidateRequest
    {
        public string ClientHash { get; set; } = "";
        public string[]? ModList { get; set; }
    }

    public class AddModEntryRequest
    {
        public string PackageId { get; set; } = "";
        public string? ModName { get; set; }
        public string? Version { get; set; }
        public string? CompatStatus { get; set; }
        public string? Notes { get; set; }
    }
}
