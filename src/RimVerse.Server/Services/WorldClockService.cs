using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using RimVerse.Server.Data;

namespace RimVerse.Server.Services
{
    public class WorldClockService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<WorldClockService> _logger;
        private long _currentWorldTick;

        public long CurrentWorldTick => _currentWorldTick;

        public WorldClockService(IServiceScopeFactory scopeFactory, ILogger<WorldClockService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WorldClockService starting...");

            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var world = await db.Worlds.FirstOrDefaultAsync(stoppingToken);
                if (world != null)
                {
                    _currentWorldTick = world.WorldTick;
                    _logger.LogInformation("Loaded world tick: {Tick}", _currentWorldTick);
                }
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
                Interlocked.Increment(ref _currentWorldTick);

                if (_currentWorldTick % 60 == 0)
                {
                    await PersistWorldTick(stoppingToken);
                }
            }
        }

        private async Task PersistWorldTick(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var world = await db.Worlds.FirstOrDefaultAsync(ct);
                if (world != null)
                {
                    world.WorldTick = _currentWorldTick;
                    await db.SaveChangesAsync(ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist world tick");
            }
        }
    }
}
