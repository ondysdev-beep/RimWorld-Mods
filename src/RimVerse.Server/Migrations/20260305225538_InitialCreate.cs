using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RimVerse.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SteamId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    PasswordHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "player"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsBanned = table.Column<bool>(type: "boolean", nullable: false),
                    BanReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Worlds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Seed = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WorldTick = table.Column<long>(type: "bigint", nullable: false),
                    Storyteller = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "Cassandra"),
                    Difficulty = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Rough"),
                    ModpackHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ConfigJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Worlds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    WorldId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DetailsJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEntries_Players_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AuditEntries_Worlds_WorldId",
                        column: x => x.WorldId,
                        principalTable: "Worlds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "pending"),
                    InitiatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfferItemsJson = table.Column<string>(type: "jsonb", nullable: true),
                    RequestItemsJson = table.Column<string>(type: "jsonb", nullable: true),
                    ScheduledWorldTick = table.Column<long>(type: "bigint", nullable: false),
                    ExpiresWorldTick = table.Column<long>(type: "bigint", nullable: false),
                    EscrowLocked = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contracts_Players_InitiatorId",
                        column: x => x.InitiatorId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contracts_Players_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contracts_Worlds_WorldId",
                        column: x => x.WorldId,
                        principalTable: "Worlds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    WorldId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "global"),
                    Content = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Players_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Worlds_WorldId",
                        column: x => x.WorldId,
                        principalTable: "Worlds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JointSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "pending"),
                    HostId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModpackHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RngSeed = table.Column<long>(type: "bigint", nullable: false),
                    MaxTick = table.Column<long>(type: "bigint", nullable: false),
                    CurrentTick = table.Column<long>(type: "bigint", nullable: false),
                    ParticipantsJson = table.Column<string>(type: "jsonb", nullable: false),
                    ReplayDataUrl = table.Column<string>(type: "text", nullable: true),
                    DeltaJson = table.Column<string>(type: "jsonb", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JointSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JointSessions_Players_HostId",
                        column: x => x.HostId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JointSessions_Worlds_WorldId",
                        column: x => x.WorldId,
                        principalTable: "Worlds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModpackEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorldId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CompatStatus = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "unknown"),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModpackEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModpackEntries_Worlds_WorldId",
                        column: x => x.WorldId,
                        principalTable: "Worlds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Settlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorldId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TileId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    LocalTick = table.Column<long>(type: "bigint", nullable: false),
                    Wealth = table.Column<float>(type: "real", nullable: false),
                    SnapshotData = table.Column<byte[]>(type: "bytea", nullable: true),
                    SnapshotAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClaimRadius = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Settlements_Players_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Settlements_Worlds_WorldId",
                        column: x => x.WorldId,
                        principalTable: "Worlds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Parcels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorldId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiverId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemsJson = table.Column<string>(type: "jsonb", nullable: false),
                    PawnsJson = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "in_transit"),
                    SendWorldTick = table.Column<long>(type: "bigint", nullable: false),
                    EtaWorldTick = table.Column<long>(type: "bigint", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parcels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parcels_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Parcels_Players_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Parcels_Players_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Parcels_Worlds_WorldId",
                        column: x => x.WorldId,
                        principalTable: "Worlds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_ActorId",
                table: "AuditEntries",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_WorldId_CreatedAt",
                table: "AuditEntries",
                columns: new[] { "WorldId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_InitiatorId",
                table: "Contracts",
                column: "InitiatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_TargetId",
                table: "Contracts",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_WorldId_Status",
                table: "Contracts",
                columns: new[] { "WorldId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderId",
                table: "ChatMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_WorldId_CreatedAt",
                table: "ChatMessages",
                columns: new[] { "WorldId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_JointSessions_HostId",
                table: "JointSessions",
                column: "HostId");

            migrationBuilder.CreateIndex(
                name: "IX_JointSessions_WorldId",
                table: "JointSessions",
                column: "WorldId");

            migrationBuilder.CreateIndex(
                name: "IX_ModpackEntries_WorldId_PackageId",
                table: "ModpackEntries",
                columns: new[] { "WorldId", "PackageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_ContractId",
                table: "Parcels",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_ReceiverId_Status",
                table: "Parcels",
                columns: new[] { "ReceiverId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_SenderId",
                table: "Parcels",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_WorldId",
                table: "Parcels",
                column: "WorldId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_SteamId",
                table: "Players",
                column: "SteamId",
                unique: true,
                filter: "\"SteamId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_OwnerId",
                table: "Settlements",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_WorldId_TileId",
                table: "Settlements",
                columns: new[] { "WorldId", "TileId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEntries");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "JointSessions");

            migrationBuilder.DropTable(
                name: "ModpackEntries");

            migrationBuilder.DropTable(
                name: "Parcels");

            migrationBuilder.DropTable(
                name: "Settlements");

            migrationBuilder.DropTable(
                name: "Contracts");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Worlds");
        }
    }
}
