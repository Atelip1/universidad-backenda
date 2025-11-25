using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversidadDB.Data;
using UniversidadDB.Models;
using UniversidadDB.Services;

namespace UniversidadDB.Controllers
{
    [ApiController]
    [Route("api/admin/notificaciones")]
    [Authorize]
    public class AdminNotificacionesController : ControllerBase
    {
        private readonly UniversidadContext _context;
        private readonly FcmService _fcm;

        public AdminNotificacionesController(UniversidadContext context, FcmService fcm)
        {
            _context = context;
            _fcm = fcm;
        }

        public class EnviarNotificacionRequest
        {
            public bool ATodos { get; set; } = false;

            // ✅ Si ATodos=true y RolId tiene valor => se envía a TODOS los usuarios con ese rol
            // Ej: RolId = 2 (Estudiantes) / RolId = 1 (Admin) - según tu BD
            public int? RolId { get; set; } = null;

            // ✅ Si ATodos=false, se usa esta lista
            public List<int>? UsuarioIds { get; set; }

            public string Titulo { get; set; } = "";
            public string Mensaje { get; set; } = "";
        }

        [HttpPost("enviar")]
        public async Task<IActionResult> Enviar([FromBody] EnviarNotificacionRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Titulo) || string.IsNullOrWhiteSpace(req.Mensaje))
                return BadRequest("Título y mensaje son obligatorios.");

            // 1) Determinar destinatarios
            List<int> userIds;

            if (req.ATodos)
            {
                var q = _context.Usuarios.AsNoTracking();

                // ⚠️ Si tu columna se llama distinto (IdRol, RolID, etc.) cámbiala aquí:
                if (req.RolId.HasValue)
                    q = q.Where(u => u.RolId == req.RolId.Value);

                userIds = await q.Select(u => u.UsuarioId).ToListAsync(); // ⚠️ Ajusta si tu llave es "Id"
            }
            else
            {
                userIds = (req.UsuarioIds ?? new List<int>())
                    .Distinct()
                    .ToList();
            }

            if (userIds.Count == 0)
                return BadRequest("No hay destinatarios.");

            // 2) Guardar notificaciones en SQL (1 fila por usuario)
            var now = DateTime.UtcNow;

            var rows = userIds.Select(uid => new NotificacionSistema
            {
                UsuarioId = uid,
                Titulo = req.Titulo.Trim(),
                Mensaje = req.Mensaje.Trim(),
                FechaCreacion = now,
                Leida = false
            }).ToList();

            _context.NotificacionesSistema.AddRange(rows);
            await _context.SaveChangesAsync();

            // 3) Buscar tokens de esos usuarios
            var tokens = await _context.DeviceTokens
                .AsNoTracking()
                .Where(d => userIds.Contains(d.UsuarioId))
                .Select(d => d.Token)
                .Distinct()
                .ToListAsync();

            // 4) Enviar Push por FCM (sin bool, contamos por try/catch)
            int enviados = 0;

            foreach (var token in tokens)
            {
                try
                {
                    await _fcm.SendToTokenAsync(
                        token,
                        req.Titulo.Trim(),
                        req.Mensaje.Trim(),
                        data: new Dictionary<string, string>
                        {
                            ["tipo"] = "sistema"
                        }
                    );
                    enviados++;
                }
                catch
                {
                    // Si quieres, aquí podrías loguear el error
                    // pero NO detenemos todo el envío si un token falla.
                }
            }

            return Ok(new
            {
                guardadas = rows.Count,
                tokens = tokens.Count,
                enviados = enviados
            });
        }
    }
}
