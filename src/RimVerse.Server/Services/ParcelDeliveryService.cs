using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using RimVerse.Server.Data;
using RimVerse.Server.Hubs;

namespace RimVerse.Server.Services
{
    public class ParcelDeliveryService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly WorldClockService _worldClock;
        private readonly IHubContext<GameHub> _hubContext;
        private readonly PlayerTracker _tracker;
        private readonly ILogger<ParcelDeliveryService> _logger;

        public ParcelDeliveryService(
            IServiceScopeFactory scopeFactory,
            WorldClockService worldClock,
            IHubContext<GameHub> hubContext,
            PlayerTracker tracker,
            ILogger<ParcelDeliveryService> logger)
        {
            _scopeFactory = scopeFactory;
            _worldClock = worldClock;
            _hubContext = hubContext;
            _tracker = tracker;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ParcelDeliveryService starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);
                await ProcessDeliveries(stoppingToken);
            }
        }

        private async Task ProcessDeliveries(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var currentTick = _worldClock.CurrentWorldTick;

                var readyParcels = await db.Parcels
                    .Include(p => p.Sender)
                    .Where(p => p.Status == "in_transit" && p.EtaWorldTick <= currentTick)
                    .ToListAsync(ct);

                foreach (var parcel in readyParcels)
                {
                    parcel.Status = "delivered";
                    parcel.DeliveredAt = DateTime.UtcNow;

                    _logger.LogInformation("Parcel {ParcelId} delivered to {ReceiverId}",
                        parcel.Id, parcel.ReceiverId);

                    var connId = _tracker.GetConnectionId(parcel.ReceiverId);
                    if (connId != null)
                    {
                        await _hubContext.Clients.Client(connId).SendAsync("ParcelDelivered", new
                        {
                            ParcelId = parcel.Id.ToString(),
                            SenderName = parcel.Sender?.DisplayName ?? "Unknown",
                            Items = parcel.ItemsJson
                        }, ct);
                    }
                }

                if (readyParcels.Count > 0)
                {
                    await db.SaveChangesAsync(ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing parcel deliveries");
            }
        }
    }
}
