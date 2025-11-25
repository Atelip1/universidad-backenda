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
    [Authorize] // ✅ requiere JWT
    public class NotificacionesController : ControllerBase
    {
        private readonly UniversidadContext _context;
        public NotificacionesController(UniversidadContext context) => _context = context;

        // ✅ GET: /api/Notificaciones/mias?soloNoLeidas=false&page=1&pageSize=30
        [HttpGet("mias")]
        public async Task<IActionResult> MisNotificaciones(
            [FromQuery] bool soloNoLeidas = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 30)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 30;
            if (pageSize > 100) pageSize = 100;

            var userId = GetUserIdOrThrow();

            var q = _context.NotificacionesSistema
                .AsNoTracking()
                .Where(n => n.UsuarioId == userId);

            if (soloNoLeidas)
                q = q.Where(n => !n.Leida);

            var total = await q.CountAsync();

            var data = await q
                .OrderByDescending(n => n.FechaCreacion)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new
                {
                    n.NotificacionId,
                    n.Titulo,
                    n.Mensaje,
                    n.FechaCreacion,
                    n.Leida
                })
                .ToListAsync();

            return Ok(new { total, page, pageSize, data });
        }

        // ✅ GET: /api/Notificaciones/unread-count
        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = GetUserIdOrThrow();

            var count = await _context.NotificacionesSistema
                .AsNoTracking()
                .Where(n => n.UsuarioId == userId && !n.Leida)
                .CountAsync();

            return Ok(new { unread = count });
        }

        // ✅ POST: /api/Notificaciones/{id}/leer
        [HttpPost("{id:int}/leer")]
        public async Task<IActionResult> MarcarLeida([FromRoute] int id)
        {
            var userId = GetUserIdOrThrow();

            var notif = await _context.NotificacionesSistema
                .FirstOrDefaultAsync(n => n.NotificacionId == id && n.UsuarioId == userId);

            if (notif == null) return NotFound("Notificación no encontrada.");

            if (!notif.Leida)
            {
                notif.Leida = true;
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Marcada como leída" });
        }

        private int GetUserIdOrThrow()
        {
            var claim =
                User.FindFirst(ClaimTypes.NameIdentifier) ??
                User.FindFirst("id") ??
                User.FindFirst("userId") ??
                User.FindFirst("sub");

            if (claim == null || !int.TryParse(claim.Value, out var userId))
                throw new UnauthorizedAccessException("No se encontró el userId en el JWT.");

            return userId;
        }
    }
}
