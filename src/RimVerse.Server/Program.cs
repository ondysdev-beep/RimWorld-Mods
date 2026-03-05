using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RimVerse.Server.Data;
using RimVerse.Server.Hubs;
using RimVerse.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// === DATABASE ===
var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? "Host=localhost;Database=rimverse;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// === AUTH (JWT) ===
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "RimVerse-Dev-Secret-Key-Change-In-Production-Min32Chars!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "rimverse-server";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "rimverse-client";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// === SERVICES ===
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<PlayerTracker>();
builder.Services.AddSingleton<WorldClockService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<WorldClockService>());
builder.Services.AddSingleton<AuditService>();
builder.Services.AddSingleton<SessionOrchestrator>();
builder.Services.AddHostedService<ParcelDeliveryService>();

// === API + SIGNALR ===
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    options.AddPolicy("SignalR", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// === AUTO MIGRATE + SEED ===
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    if (!await db.Worlds.AnyAsync())
    {
        db.Worlds.Add(new RimVerse.Server.Data.Entities.World
        {
            Id = Guid.NewGuid(),
            Name = "RimVerse Alpha",
            Seed = "rimverse-alpha-2026",
            WorldTick = 0,
            Storyteller = "Cassandra",
            Difficulty = "Rough",
            ModpackHash = "none",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}

// === MIDDLEWARE ===
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("SignalR");
app.UseAuthentication();
app.UseAuthorization();

// === ENDPOINTS ===
app.MapControllers();
app.MapHub<GameHub>("/hubs/game");

app.MapGet("/health", () => Results.Ok(new
{
    Status = "healthy",
    Service = "RimVerse Server",
    Version = "0.1.0",
    Timestamp = DateTime.UtcNow
}));

app.Run();
