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

        // Obtener notificaciones del usuario (Estudiantes)
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

        // Obtener el número de notificaciones no leídas para un usuario (Estudiantes)
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

        // Crear una nueva notificación (Solo el Administrador puede enviar notificaciones)
        [HttpPost]
        [Authorize(Roles = "ADMIN")] // Solo los administradores pueden crear notificaciones
        public async Task<IActionResult> CrearNotificacion([FromBody] NotificacionDto notificacionDto)
        {
            // Verificar si el usuario que realiza la petición tiene privilegios de ADMIN
            var userId = GetUserIdOrThrow();  // Este método asegura que el que hace la petición es un admin

            // Crear la nueva notificación
            var newNotification = new Notificacion
            {
                Titulo = notificacionDto.Titulo,
                Mensaje = notificacionDto.Mensaje,
                FechaCreacion = DateTime.UtcNow,
                Leida = false,
                Tipo = notificacionDto.Tipo,
                Canal = notificacionDto.Canal
            };

            // Obtener todos los usuarios para enviarles la notificación
            var usuarios = await _context.Usuarios.ToListAsync();

            // Crear una notificación para cada usuario
            foreach (var usuario in usuarios)
            {
                var userNotification = new Notificacion
                {
                    UsuarioId = usuario.UsuarioId, // Asignamos la notificación a cada usuario
                    Titulo = newNotification.Titulo,
                    Mensaje = newNotification.Mensaje,
                    FechaCreacion = DateTime.UtcNow,
                    Leida = false,
                    Tipo = newNotification.Tipo,
                    Canal = newNotification.Canal
                };

                _context.Notificaciones.Add(userNotification);  // Añadir la notificación a la base de datos
            }

            // Guardar todas las notificaciones a la base de datos
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notificación enviada a todos los usuarios" });
        }

        private int GetUserIdOrThrow()
        {
            // Obtiene el ID del usuario desde el token JWT para asegurarse de que sea un ADMIN
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
