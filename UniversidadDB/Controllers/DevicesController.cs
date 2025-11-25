using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UniversidadDB.Data;
using UniversidadDB.Models;

namespace UniversidadDB.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly UniversidadContext _context;
        public DevicesController(UniversidadContext context) => _context = context;

        public class RegisterTokenRequest
        {
            public string Token { get; set; } = string.Empty;
            public string? Platform { get; set; }
        }

        // ✅ Recomendado: protegido con JWT
        [Authorize]
        [HttpPost("token")]
        public async Task<IActionResult> RegisterToken([FromBody] RegisterTokenRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Token))
                return BadRequest("Token requerido.");

            var userId = GetUserIdOrThrow();

            // Upsert por Token (token único)
            var existing = await _context.DeviceTokens.FirstOrDefaultAsync(x => x.Token == req.Token);

            if (existing == null)
            {
                existing = new DeviceToken
                {
                    UsuarioId = userId,
                    Token = req.Token.Trim(),
                    Platform = req.Platform,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.DeviceTokens.Add(existing);
            }
            else
            {
                existing.UsuarioId = userId;
                existing.Platform = req.Platform;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Token registrado" });
        }

        private int GetUserIdOrThrow()
        {
            var claim =
                User.FindFirst(ClaimTypes.NameIdentifier) ??
                User.FindFirst("id") ??
                User.FindFirst("userId") ??
                User.FindFirst("sub");

            if (claim == null || !int.TryParse(claim.Value, out var userId))
                throw new UnauthorizedAccessException("No se encontró el userId en el token JWT.");

            return userId;
        }
    }
}
