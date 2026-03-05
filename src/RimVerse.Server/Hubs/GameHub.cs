using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RimVerse.Server.Data;
using RimVerse.Server.Data.Entities;
using RimVerse.Server.Services;

namespace RimVerse.Server.Hubs
{
    [Authorize]
    public class GameHub : Hub
    {
        private readonly AppDbContext _db;
        private readonly PlayerTracker _tracker;
        private readonly WorldClockService _worldClock;
        private readonly AuditService _audit;
        private readonly ILogger<GameHub> _logger;

        public GameHub(
            AppDbContext db,
            PlayerTracker tracker,
            WorldClockService worldClock,
            AuditService audit,
            ILogger<GameHub> logger)
        {
            _db = db;
            _tracker = tracker;
            _worldClock = worldClock;
            _audit = audit;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var playerId = GetPlayerId();
            if (playerId == null)
            {
                Context.Abort();
                return;
            }

            _tracker.PlayerConnected(playerId.Value, Context.ConnectionId);

            var player = await _db.Players.FindAsync(playerId.Value);
            if (player != null)
            {
                player.LastSeenAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                await Clients.Others.SendAsync("PlayerJoined", new
                {
                    PlayerId = player.Id.ToString(),
                    DisplayName = player.DisplayName
                });

                _logger.LogInformation("Player {Name} ({Id}) connected", player.DisplayName, player.Id);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var playerId = GetPlayerId();
            if (playerId != null)
            {
                _tracker.PlayerDisconnected(playerId.Value);

                var player = await _db.Players.FindAsync(playerId.Value);
                if (player != null)
                {
                    await Clients.Others.SendAsync("PlayerLeft", new
                    {
                        PlayerId = player.Id.ToString(),
                        DisplayName = player.DisplayName
                    });

                    _logger.LogInformation("Player {Name} ({Id}) disconnected", player.DisplayName, player.Id);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendChatMessage(string channel, string content)
        {
            var playerId = GetPlayerId();
            if (playerId == null) return;

            if (string.IsNullOrWhiteSpace(content) || content.Length > 500) return;

            var player = await _db.Players.FindAsync(playerId.Value);
            if (player == null) return;

            var world = await _db.Worlds.FirstOrDefaultAsync();
            if (world == null) return;

            var msg = new ChatMessage
            {
                WorldId = world.Id,
                SenderId = playerId.Value,
                Channel = channel ?? "global",
                Content = content,
                CreatedAt = DateTime.UtcNow
            };
            _db.ChatMessages.Add(msg);
            await _db.SaveChangesAsync();

            await Clients.All.SendAsync("ReceiveChatMessage", new
            {
                SenderId = player.Id.ToString(),
                SenderName = player.DisplayName,
                Channel = msg.Channel,
                Content = msg.Content,
                Timestamp = msg.CreatedAt
            });
        }

        public async Task RequestWorldSync()
        {
            await Clients.Caller.SendAsync("WorldClockSync", new
            {
                WorldTick = _worldClock.CurrentWorldTick,
                ServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }

        public async Task Heartbeat()
        {
            await Clients.Caller.SendAsync("HeartbeatAck", new
            {
                ServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }

        private Guid? GetPlayerId()
        {
            var sub = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? Context.User?.FindFirst("sub")?.Value;
            return sub != null && Guid.TryParse(sub, out var id) ? id : null;
        }
    }
}
