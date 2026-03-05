using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RimVerse.Server.Data;
using RimVerse.Server.Data.Entities;
using RimVerse.Server.Models;
using RimVerse.Server.Services;

namespace RimVerse.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly JwtService _jwt;
        private readonly AuditService _audit;

        public AuthController(AppDbContext db, JwtService jwt, AuditService audit)
        {
            _db = db;
            _jwt = jwt;
            _audit = audit;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length > 64)
                return BadRequest("Username must be 1-64 characters.");

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
                return BadRequest("Password must be at least 6 characters.");

            var exists = await _db.Players.AnyAsync(p => p.DisplayName == request.Username);
            if (exists)
                return Conflict("Username already taken.");

            var player = new Player
            {
                Id = Guid.NewGuid(),
                DisplayName = request.Username,
                PasswordHash = HashPassword(request.Password),
                SteamId = request.SteamId,
                Role = "player",
                CreatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow
            };

            _db.Players.Add(player);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(null, player.Id, "player.register");

            var token = _jwt.GenerateToken(player.Id, player.DisplayName, player.Role);

            return Ok(new AuthResponse
            {
                Token = token,
                PlayerId = player.Id.ToString(),
                DisplayName = player.DisplayName,
                ExpiresAt = _jwt.GetExpiration()
            });
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            var player = await _db.Players.FirstOrDefaultAsync(p => p.DisplayName == request.Username);
            if (player == null)
                return Unauthorized("Invalid username or password.");

            if (player.PasswordHash != HashPassword(request.Password))
                return Unauthorized("Invalid username or password.");

            if (player.IsBanned)
                return Forbid($"Account banned: {player.BanReason}");

            player.LastSeenAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(null, player.Id, "player.login");

            var token = _jwt.GenerateToken(player.Id, player.DisplayName, player.Role);

            return Ok(new AuthResponse
            {
                Token = token,
                PlayerId = player.Id.ToString(),
                DisplayName = player.DisplayName,
                ExpiresAt = _jwt.GetExpiration()
            });
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
