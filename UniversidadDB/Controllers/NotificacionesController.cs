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
    [Authorize] // Requiere JWT
    public class NotificacionesController : ControllerBase
    {
        private readonly UniversidadContext _context;

        public NotificacionesController(UniversidadContext context) => _context = context;

        // Obtener notificaciones del usuario
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

            var query = _context.Notificaciones
                .AsNoTracking()
                .Where(n => n.UsuarioId == userId);

            if (soloNoLeidas)
                query = query.Where(n => !n.Leida);

            var total = await query.CountAsync();

            var notifications = await query
                .OrderByDescending(n => n.FechaCreacion)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new
                {
                    n.NotificacionId,
                    n.Titulo,
                    n.Mensaje,
                    n.FechaCreacion,
                    n.Leida,
                    n.Tipo,
                    n.Canal
                })
                .ToListAsync();

            return Ok(new { total, page, pageSize, notifications });
        }

        // Obtener el número de notificaciones no leídas
        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = GetUserIdOrThrow();

            var unreadCount = await _context.Notificaciones
                .AsNoTracking()
                .Where(n => n.UsuarioId == userId && !n.Leida)
                .CountAsync();

            return Ok(new { unread = unreadCount });
        }

        // Marcar una notificación como leída
        [HttpPost("{id:int}/leer")]
        public async Task<IActionResult> MarcarLeida([FromRoute] int id)
        {
            var userId = GetUserIdOrThrow();

            var notif = await _context.Notificaciones
                .FirstOrDefaultAsync(n => n.NotificacionId == id && n.UsuarioId == userId);

            if (notif == null) return NotFound("Notificación no encontrada.");

            if (!notif.Leida)
            {
                notif.Leida = true;
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Notificación marcada como leída" });
        }

        // Crear una nueva notificación
        [HttpPost]
        public async Task<IActionResult> CrearNotificacion([FromBody] NotificacionDto notificacionDto)
        {
            var userId = GetUserIdOrThrow();

            var newNotification = new Notificacion
            {
                UsuarioId = userId,
                Titulo = notificacionDto.Titulo,
                Mensaje = notificacionDto.Mensaje,
                FechaCreacion = DateTime.UtcNow,
                Leida = false,
                Tipo = notificacionDto.Tipo,
                Canal = notificacionDto.Canal
            };

            _context.Notificaciones.Add(newNotification);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(MisNotificaciones), new { id = newNotification.NotificacionId }, newNotification);
        }

        private int GetUserIdOrThrow()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ??
                        User.FindFirst("id") ??
                        User.FindFirst("userId") ??
                        User.FindFirst("sub");

            if (claim == null || !int.TryParse(claim.Value, out var userId))
                throw new UnauthorizedAccessException("No se encontró el userId en el JWT.");

            return userId;
        }
    }
}
