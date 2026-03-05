using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RimVerse.Server.Hubs;
using RimVerse.Server.Services;

namespace RimVerse.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SessionController : ControllerBase
    {
        private readonly SessionOrchestrator _orchestrator;
        private readonly IHubContext<GameHub> _hubContext;
        private readonly PlayerTracker _tracker;
        private readonly AuditService _audit;

        public SessionController(
            SessionOrchestrator orchestrator,
            IHubContext<GameHub> hubContext,
            PlayerTracker tracker,
            AuditService audit)
        {
            _orchestrator = orchestrator;
            _hubContext = hubContext;
            _tracker = tracker;
            _audit = audit;
        }

        [HttpPost("request")]
        public async Task<ActionResult> RequestSession([FromBody] SessionRequestDto request)
        {
            var playerId = GetPlayerId();
            if (playerId == null) return Unauthorized();

            if (!Guid.TryParse(request.TargetPlayerId, out var targetId))
                return BadRequest("Invalid target player ID.");

            var session = await _orchestrator.RequestSession(playerId.Value, targetId, request.SessionType ?? "visit");
            if (session == null)
                return BadRequest("Cannot create session. Target may be offline or already in a session.");

            var targetConnId = _tracker.GetConnectionId(targetId);
            if (targetConnId != null)
            {
                await _hubContext.Clients.Client(targetConnId).SendAsync("SessionInvite", new
                {
                    SessionId = session.Id.ToString(),
                    InitiatorId = playerId.Value.ToString(),
                    SessionType = session.Type
                });
            }

            await _audit.LogAsync(session.WorldId, playerId, "session.request",
                $"{{\"sessionId\":\"{session.Id}\",\"target\":\"{targetId}\"}}");

            return Ok(new { SessionId = session.Id.ToString(), Status = "pending" });
        }

        [HttpPost("{id}/accept")]
        public async Task<ActionResult> AcceptSession(Guid id)
        {
            var playerId = GetPlayerId();
            if (playerId == null) return Unauthorized();

            var success = await _orchestrator.AcceptSession(id, playerId.Value);
            if (!success) return BadRequest("Cannot accept session.");

            var active = _orchestrator.GetActiveSession(id);
            if (active != null)
            {
                foreach (var pid in active.ParticipantIds)
                {
                    var connId = _tracker.GetConnectionId(pid);
                    if (connId != null)
                    {
                        await _hubContext.Clients.Client(connId).SendAsync("SessionStart", new
                        {
                            SessionId = id.ToString(),
                            ParticipantIds = active.ParticipantIds.ConvertAll(p => p.ToString()),
                            RngSeed = 0L
                        });
                    }
                }
            }

            await _audit.LogAsync(null, playerId, "session.accept", $"{{\"sessionId\":\"{id}\"}}");
            return Ok(new { message = "Session started." });
        }

        [HttpPost("{id}/reject")]
        public async Task<ActionResult> RejectSession(Guid id)
        {
            var playerId = GetPlayerId();
            if (playerId == null) return Unauthorized();

            var success = await _orchestrator.RejectSession(id, playerId.Value);
            if (!success) return BadRequest("Cannot reject session.");

            await _audit.LogAsync(null, playerId, "session.reject", $"{{\"sessionId\":\"{id}\"}}");
            return Ok(new { message = "Session rejected." });
        }

        [HttpPost("{id}/end")]
        public async Task<ActionResult> EndSession(Guid id, [FromBody] EndSessionDto? dto)
        {
            var playerId = GetPlayerId();
            if (playerId == null) return Unauthorized();

            await _orchestrator.EndSession(id, dto?.DeltaJson);

            await _audit.LogAsync(null, playerId, "session.end", $"{{\"sessionId\":\"{id}\"}}");
            return Ok(new { message = "Session ended." });
        }

        private Guid? GetPlayerId()
        {
            var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst("sub")?.Value;
            return sub != null && Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public class SessionRequestDto
    {
        public string TargetPlayerId { get; set; } = "";
        public string? SessionType { get; set; }
    }

    public class EndSessionDto
    {
        public string? DeltaJson { get; set; }
    }
}
