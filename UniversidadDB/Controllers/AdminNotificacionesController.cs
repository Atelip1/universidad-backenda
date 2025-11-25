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
    [Authorize] // luego puedes restringir a admin si ya manejas roles
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
            public List<int> UsuarioIds { get; set; } = new();
            public string Titulo { get; set; } = "";
            public string Mensaje { get; set; } = "";
        }

        [HttpPost("enviar")]
        public async Task<IActionResult> Enviar([FromBody] EnviarNotificacionRequest req)
        {
            if (req.UsuarioIds == null || req.UsuarioIds.Count == 0)
                return BadRequest("UsuarioIds requerido.");

            if (string.IsNullOrWhiteSpace(req.Titulo) || string.IsNullOrWhiteSpace(req.Mensaje))
                return BadRequest("Titulo y Mensaje son obligatorios.");

            // 1) Guardar en SQL (una notificación por usuario)
            var now = DateTime.UtcNow;
            var rows = req.UsuarioIds.Select(uid => new NotificacionSistema
            {
                UsuarioId = uid,
                Titulo = req.Titulo.Trim(),
                Mensaje = req.Mensaje.Trim(),
                FechaCreacion = now,
                Leida = false
            }).ToList();

            _context.NotificacionesSistema.AddRange(rows);
            await _context.SaveChangesAsync();

            // 2) Buscar tokens de esos usuarios
            var tokens = await _context.DeviceTokens
                .Where(d => req.UsuarioIds.Contains(d.UsuarioId))
                .Select(d => d.Token)
                .Distinct()
                .ToListAsync();

            // 3) Enviar push por FCM
            foreach (var token in tokens)
            {
                await _fcm.SendToTokenAsync(
                    token,
                    req.Titulo,
                    req.Mensaje,
                    data: new Dictionary<string, string>
                    {
                        ["tipo"] = "sistema"
                    }
                );
            }

            return Ok(new { guardadas = rows.Count, tokens = tokens.Count });
        }
    }
}
