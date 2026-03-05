using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RimVerse.Server.Data;
using RimVerse.Server.Data.Entities;

namespace RimVerse.Server.Services
{
    public class SessionOrchestrator
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly PlayerTracker _tracker;
        private readonly ILogger<SessionOrchestrator> _logger;
        private readonly ConcurrentDictionary<Guid, ActiveSession> _activeSessions = new();

        public SessionOrchestrator(
            IServiceScopeFactory scopeFactory,
            PlayerTracker tracker,
            ILogger<SessionOrchestrator> logger)
        {
            _scopeFactory = scopeFactory;
            _tracker = tracker;
            _logger = logger;
        }

        public async Task<JointSession?> RequestSession(Guid initiatorId, Guid targetId, string sessionType)
        {
            if (!_tracker.IsOnline(targetId))
            {
                _logger.LogWarning("Session request failed: target {TargetId} is offline", targetId);
                return null;
            }

            if (_activeSessions.Values.Any(s =>
                s.ParticipantIds.Contains(initiatorId) || s.ParticipantIds.Contains(targetId)))
            {
                _logger.LogWarning("Session request failed: one of the players is already in a session");
                return null;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var world = await db.Worlds.FirstOrDefaultAsync();
            if (world == null) return null;

            var session = new JointSession
            {
                Id = Guid.NewGuid(),
                WorldId = world.Id,
                Type = sessionType,
                Status = "pending",
                HostId = targetId,
                ModpackHash = world.ModpackHash,
                RngSeed = new Random().NextInt64(),
                MaxTick = 60000,
                CurrentTick = 0,
                ParticipantsJson = System.Text.Json.JsonSerializer.Serialize(new[]
                {
                    new { PlayerId = initiatorId.ToString(), Role = "visitor", Ready = false },
                    new { PlayerId = targetId.ToString(), Role = "host", Ready = false }
                }),
                CreatedAt = DateTime.UtcNow
            };

            db.JointSessions.Add(session);
            await db.SaveChangesAsync();

            _logger.LogInformation("Session {SessionId} created: {Initiator} -> {Target} ({Type})",
                session.Id, initiatorId, targetId, sessionType);

            return session;
        }

        public async Task<bool> AcceptSession(Guid sessionId, Guid playerId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var session = await db.JointSessions.FindAsync(sessionId);
            if (session == null || session.Status != "pending") return false;
            if (session.HostId != playerId) return false;

            session.Status = "active";
            session.StartedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var participantIds = ExtractParticipantIds(session.ParticipantsJson);

            _activeSessions[sessionId] = new ActiveSession
            {
                SessionId = sessionId,
                ParticipantIds = participantIds,
                CurrentTick = 0,
                TickHashes = new ConcurrentDictionary<long, Dictionary<Guid, uint>>(),
                PendingInputs = new ConcurrentDictionary<long, Dictionary<Guid, byte[]>>()
            };

            _logger.LogInformation("Session {SessionId} started", sessionId);
            return true;
        }

        public async Task<bool> RejectSession(Guid sessionId, Guid playerId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var session = await db.JointSessions.FindAsync(sessionId);
            if (session == null || session.Status != "pending") return false;
            if (session.HostId != playerId) return false;

            session.Status = "aborted";
            session.EndedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            _logger.LogInformation("Session {SessionId} rejected by host", sessionId);
            return true;
        }

        public TickResult? SubmitTick(Guid sessionId, Guid playerId, long tick, byte[] inputs, uint stateHash)
        {
            if (!_activeSessions.TryGetValue(sessionId, out var active)) return null;

            var tickInputs = active.PendingInputs.GetOrAdd(tick, _ => new Dictionary<Guid, byte[]>());
            var tickHashes = active.TickHashes.GetOrAdd(tick, _ => new Dictionary<Guid, uint>());

            lock (tickInputs)
            {
                tickInputs[playerId] = inputs;
                tickHashes[playerId] = stateHash;
            }

            if (tickInputs.Count >= active.ParticipantIds.Count)
            {
                var hashes = tickHashes.Values.Distinct().ToList();
                var desync = hashes.Count > 1;

                var mergedInputs = new List<byte>();
                foreach (var participantId in active.ParticipantIds)
                {
                    if (tickInputs.TryGetValue(participantId, out var input))
                    {
                        mergedInputs.AddRange(BitConverter.GetBytes(input.Length));
                        mergedInputs.AddRange(input);
                    }
                }

                active.CurrentTick = tick;

                active.PendingInputs.TryRemove(tick, out _);
                active.TickHashes.TryRemove(tick, out _);

                return new TickResult
                {
                    Tick = tick,
                    MergedInputs = mergedInputs.ToArray(),
                    Desync = desync,
                    HashValues = tickHashes.ToDictionary(kv => kv.Key, kv => kv.Value)
                };
            }

            return null;
        }

        public async Task EndSession(Guid sessionId, string? deltaJson = null)
        {
            _activeSessions.TryRemove(sessionId, out _);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var session = await db.JointSessions.FindAsync(sessionId);
            if (session == null) return;

            session.Status = "completed";
            session.EndedAt = DateTime.UtcNow;
            session.DeltaJson = deltaJson;
            await db.SaveChangesAsync();

            _logger.LogInformation("Session {SessionId} ended", sessionId);
        }

        public ActiveSession? GetActiveSession(Guid sessionId)
        {
            _activeSessions.TryGetValue(sessionId, out var session);
            return session;
        }

        public bool IsPlayerInSession(Guid playerId)
        {
            return _activeSessions.Values.Any(s => s.ParticipantIds.Contains(playerId));
        }

        private List<Guid> ExtractParticipantIds(string json)
        {
            try
            {
                var participants = System.Text.Json.JsonSerializer.Deserialize<List<ParticipantEntry>>(json);
                return participants?.Select(p => Guid.Parse(p.PlayerId)).ToList() ?? new List<Guid>();
            }
            catch
            {
                return new List<Guid>();
            }
        }

        private class ParticipantEntry
        {
            public string PlayerId { get; set; } = "";
            public string Role { get; set; } = "";
            public bool Ready { get; set; }
        }
    }

    public class ActiveSession
    {
        public Guid SessionId { get; set; }
        public List<Guid> ParticipantIds { get; set; } = new();
        public long CurrentTick { get; set; }
        public ConcurrentDictionary<long, Dictionary<Guid, uint>> TickHashes { get; set; } = new();
        public ConcurrentDictionary<long, Dictionary<Guid, byte[]>> PendingInputs { get; set; } = new();
    }

    public class TickResult
    {
        public long Tick { get; set; }
        public byte[] MergedInputs { get; set; } = Array.Empty<byte>();
        public bool Desync { get; set; }
        public Dictionary<Guid, uint> HashValues { get; set; } = new();
    }
}
