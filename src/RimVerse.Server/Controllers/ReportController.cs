using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RimVerse.Server.Services;

namespace RimVerse.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly AuditService _audit;
        private readonly ILogger<ReportController> _logger;

        public ReportController(AuditService audit, ILogger<ReportController> logger)
        {
            _audit = audit;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult> SubmitReport([FromBody] BugReportRequest request)
        {
            var playerId = GetPlayerId();
            if (playerId == null) return Unauthorized();

            _logger.LogWarning(
                "Bug report from {PlayerId}: [{Category}] {Description} | GameVer={GameVersion} ModHash={ModpackHash} Mods={ModCount}",
                playerId, request.Category, request.Description,
                request.GameVersion, request.ModpackHash, request.ModList?.Length ?? 0);

            await _audit.LogAsync(null, playerId, "report.submit",
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    request.Category,
                    request.Description,
                    request.GameVersion,
                    request.ModpackHash,
                    ModCount = request.ModList?.Length ?? 0,
                    request.SessionReplayId
                }));

            return Ok(new
            {
                ReportId = Guid.NewGuid().ToString(),
                Message = "Report received. Thank you!"
            });
        }

        private Guid? GetPlayerId()
        {
            var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst("sub")?.Value;
            return sub != null && Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public class BugReportRequest
    {
        public string Category { get; set; } = "Bug";
        public string Description { get; set; } = "";
        public string? GameVersion { get; set; }
        public string? ModpackHash { get; set; }
        public string[]? ModList { get; set; }
        public string? SessionReplayId { get; set; }
        public string? LogTail { get; set; }
    }
}
