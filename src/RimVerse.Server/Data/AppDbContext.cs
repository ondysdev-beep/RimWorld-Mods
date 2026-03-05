using Microsoft.EntityFrameworkCore;
using RimVerse.Server.Data.Entities;

namespace RimVerse.Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Player> Players => Set<Player>();
        public DbSet<World> Worlds => Set<World>();
        public DbSet<Settlement> Settlements => Set<Settlement>();
        public DbSet<Contract> Contracts => Set<Contract>();
        public DbSet<Parcel> Parcels => Set<Parcel>();
        public DbSet<JointSession> JointSessions => Set<JointSession>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
        public DbSet<ModpackEntry> ModpackEntries => Set<ModpackEntry>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // === PLAYER ===
            modelBuilder.Entity<Player>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.DisplayName).HasMaxLength(64).IsRequired();
                e.Property(p => p.SteamId).HasMaxLength(32);
                e.HasIndex(p => p.SteamId).IsUnique().HasFilter("\"SteamId\" IS NOT NULL");
                e.Property(p => p.PasswordHash).HasMaxLength(128).IsRequired();
                e.Property(p => p.Role).HasMaxLength(16).HasDefaultValue("player");
            });

            // === WORLD ===
            modelBuilder.Entity<World>(e =>
            {
                e.HasKey(w => w.Id);
                e.Property(w => w.Name).HasMaxLength(128).IsRequired();
                e.Property(w => w.Seed).HasMaxLength(64).IsRequired();
                e.Property(w => w.Storyteller).HasMaxLength(64).HasDefaultValue("Cassandra");
                e.Property(w => w.Difficulty).HasMaxLength(32).HasDefaultValue("Rough");
                e.Property(w => w.ModpackHash).HasMaxLength(64).IsRequired();
                e.Property(w => w.ConfigJson).HasColumnType("jsonb");
            });

            // === SETTLEMENT ===
            modelBuilder.Entity<Settlement>(e =>
            {
                e.HasKey(s => s.Id);
                e.HasIndex(s => new { s.WorldId, s.TileId }).IsUnique();
                e.HasIndex(s => s.OwnerId);
                e.HasOne(s => s.World).WithMany().HasForeignKey(s => s.WorldId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(s => s.Owner).WithMany(p => p.Settlements).HasForeignKey(s => s.OwnerId).OnDelete(DeleteBehavior.Cascade);
                e.Property(s => s.Name).HasMaxLength(128);
            });

            // === CONTRACT ===
            modelBuilder.Entity<Contract>(e =>
            {
                e.HasKey(c => c.Id);
                e.HasIndex(c => new { c.WorldId, c.Status });
                e.Property(c => c.Type).HasMaxLength(32).IsRequired();
                e.Property(c => c.Status).HasMaxLength(32).HasDefaultValue("pending");
                e.Property(c => c.OfferItemsJson).HasColumnType("jsonb");
                e.Property(c => c.RequestItemsJson).HasColumnType("jsonb");
                e.HasOne(c => c.World).WithMany().HasForeignKey(c => c.WorldId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(c => c.Initiator).WithMany().HasForeignKey(c => c.InitiatorId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(c => c.Target).WithMany().HasForeignKey(c => c.TargetId).OnDelete(DeleteBehavior.Restrict);
            });

            // === PARCEL ===
            modelBuilder.Entity<Parcel>(e =>
            {
                e.HasKey(p => p.Id);
                e.HasIndex(p => new { p.ReceiverId, p.Status });
                e.Property(p => p.ItemsJson).HasColumnType("jsonb").IsRequired();
                e.Property(p => p.PawnsJson).HasColumnType("jsonb");
                e.Property(p => p.Status).HasMaxLength(32).HasDefaultValue("in_transit");
                e.HasOne(p => p.Contract).WithMany().HasForeignKey(p => p.ContractId).OnDelete(DeleteBehavior.SetNull);
                e.HasOne(p => p.World).WithMany().HasForeignKey(p => p.WorldId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(p => p.Sender).WithMany().HasForeignKey(p => p.SenderId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(p => p.Receiver).WithMany().HasForeignKey(p => p.ReceiverId).OnDelete(DeleteBehavior.Restrict);
            });

            // === JOINT SESSION ===
            modelBuilder.Entity<JointSession>(e =>
            {
                e.HasKey(j => j.Id);
                e.Property(j => j.Type).HasMaxLength(32).IsRequired();
                e.Property(j => j.Status).HasMaxLength(32).HasDefaultValue("pending");
                e.Property(j => j.ModpackHash).HasMaxLength(64).IsRequired();
                e.Property(j => j.ParticipantsJson).HasColumnType("jsonb").IsRequired();
                e.Property(j => j.DeltaJson).HasColumnType("jsonb");
                e.HasOne(j => j.World).WithMany().HasForeignKey(j => j.WorldId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(j => j.Host).WithMany().HasForeignKey(j => j.HostId).OnDelete(DeleteBehavior.Restrict);
            });

            // === CHAT MESSAGE ===
            modelBuilder.Entity<ChatMessage>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Id).UseIdentityAlwaysColumn();
                e.HasIndex(c => new { c.WorldId, c.CreatedAt });
                e.Property(c => c.Channel).HasMaxLength(32).HasDefaultValue("global");
                e.Property(c => c.Content).HasMaxLength(500).IsRequired();
                e.HasOne(c => c.World).WithMany().HasForeignKey(c => c.WorldId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(c => c.Sender).WithMany().HasForeignKey(c => c.SenderId).OnDelete(DeleteBehavior.Restrict);
            });

            // === AUDIT LOG ===
            modelBuilder.Entity<AuditEntry>(e =>
            {
                e.HasKey(a => a.Id);
                e.Property(a => a.Id).UseIdentityAlwaysColumn();
                e.HasIndex(a => new { a.WorldId, a.CreatedAt });
                e.Property(a => a.Action).HasMaxLength(64).IsRequired();
                e.Property(a => a.DetailsJson).HasColumnType("jsonb");
                e.HasOne(a => a.World).WithMany().HasForeignKey(a => a.WorldId).OnDelete(DeleteBehavior.SetNull);
                e.HasOne(a => a.Actor).WithMany().HasForeignKey(a => a.ActorId).OnDelete(DeleteBehavior.SetNull);
            });

            // === MODPACK ENTRY ===
            modelBuilder.Entity<ModpackEntry>(e =>
            {
                e.HasKey(m => m.Id);
                e.HasIndex(m => new { m.WorldId, m.PackageId }).IsUnique();
                e.Property(m => m.PackageId).HasMaxLength(256).IsRequired();
                e.Property(m => m.ModName).HasMaxLength(256);
                e.Property(m => m.Version).HasMaxLength(32);
                e.Property(m => m.CompatStatus).HasMaxLength(16).HasDefaultValue("unknown");
                e.HasOne(m => m.World).WithMany().HasForeignKey(m => m.WorldId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
