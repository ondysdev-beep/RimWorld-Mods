using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RimVerse.Server.Services
{
    public class PlayerTracker
    {
        private readonly ConcurrentDictionary<Guid, string> _onlinePlayers = new();

        public void PlayerConnected(Guid playerId, string connectionId)
        {
            _onlinePlayers[playerId] = connectionId;
        }

        public void PlayerDisconnected(Guid playerId)
        {
            _onlinePlayers.TryRemove(playerId, out _);
        }

        public bool IsOnline(Guid playerId) => _onlinePlayers.ContainsKey(playerId);

        public string? GetConnectionId(Guid playerId)
        {
            _onlinePlayers.TryGetValue(playerId, out var connId);
            return connId;
        }

        public int OnlineCount => _onlinePlayers.Count;

        public IReadOnlyList<Guid> GetOnlinePlayerIds() => _onlinePlayers.Keys.ToList();
    }
}
