using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RimVerse.Server.Data;
using RimVerse.Server.Data.Entities;

namespace RimVerse.Server.Services
{
    public class AuditService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public AuditService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task LogAsync(Guid? worldId, Guid? actorId, string action, string? detailsJson = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.AuditEntries.Add(new AuditEntry
            {
                WorldId = worldId,
                ActorId = actorId,
                Action = action,
                DetailsJson = detailsJson,
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }
    }
}
