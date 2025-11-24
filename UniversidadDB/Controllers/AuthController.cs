using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UniversidadDB.Data;
using UniversidadDB.Models;
using UniversidadDB.Models.Auth;
using UniversidadDB.Data.Auth;

namespace UniversidadDB.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UniversidadContext _context;
        private readonly EmailService _emailService;

        // Inyectamos el contexto de la base de datos y el servicio de correo electrónico
        public AuthController(UniversidadContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ===================== LOGIN =====================
        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Email y contraseña son obligatorios.");
            }

            var user = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Estudiante)
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Activo);

            if (user == null)
            {
                return Unauthorized("Usuario no encontrado o inactivo.");
            }

            // ⚠️ Usamos hash para la comparación de la contraseña
            if (!VerifyPasswordHash(request.Password, user.PasswordHash))
            {
                return Unauthorized("Contraseña incorrecta.");
            }

            var response = new LoginResponse
            {
                UsuarioId = user.UsuarioId,
                NombreCompleto = user.NombreCompleto,
                Email = user.Email,
                Rol = user.Rol.NombreRol, // "ADMIN" o "ESTUDIANTE"
                EstudianteId = user.Estudiante?.EstudianteId,
                Carrera = user.Estudiante?.Carrera,
                CodigoEstudiante = user.Estudiante?.CodigoEstudiante,
                Ciclo = user.Estudiante?.Ciclo,
                Message = "Login correcto."
            };

            return Ok(response);
        }

        // ===================== REGISTRO ESTUDIANTE =====================
        // POST: api/Auth/register-estudiante
        [HttpPost("register-estudiante")]
        public async Task<ActionResult<LoginResponse>> RegisterEstudiante([FromBody] RegisterEstudianteRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // (Opcional) exigir correo institucional
            // if (!request.Email.EndsWith("@upsjb.edu.pe", StringComparison.OrdinalIgnoreCase))
            // {
            //     return BadRequest("Debes usar tu correo institucional (@upsjb.edu.pe).");
            // }

            // 1. Verificar que no exista ya el email
            var existing = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existing != null)
            {
                return Conflict("Ya existe un usuario con ese email.");
            }

            // 2. Obtener Rol ESTUDIANTE
            var rolEstudiante = await _context.Roles.FirstOrDefaultAsync(r => r.NombreRol == "ESTUDIANTE");
            if (rolEstudiante == null)
            {
                return StatusCode(500, "No se encuentra el rol ESTUDIANTE en la base de datos.");
            }

            // 3. Crear Usuario con contraseña encriptada
            var usuario = new Usuario
            {
                NombreCompleto = request.NombreCompleto,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),  // Hasheamos la contraseña
                RolId = rolEstudiante.RolId,
                Activo = true,
                FechaRegistro = DateTime.Now
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync(); // Guarda para tener UsuarioId

            // 4. Crear Estudiante (1:1 con Usuario)
            var estudiante = new Estudiante
            {
                EstudianteId = usuario.UsuarioId,
                CodigoEstudiante = request.CodigoEstudiante,
                Carrera = request.Carrera,
                Ciclo = request.Ciclo,
                FechaIngreso = DateTime.Now
            };

            _context.Estudiantes.Add(estudiante);
            await _context.SaveChangesAsync();

            // 5. Enviar correo de bienvenida
            try
            {
                await _emailService.SendWelcomeEmail(request.Email, request.NombreCompleto);
            }
            catch (Exception ex)
            {
                // Si falla el correo, el usuario igual queda registrado
                return StatusCode(500, $"Error al enviar el correo: {ex.Message}");
            }

            // Respuesta de éxito
            var response = new LoginResponse
            {
                UsuarioId = usuario.UsuarioId,
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                Rol = "ESTUDIANTE",
                EstudianteId = estudiante.EstudianteId,
                Message = "Estudiante registrado correctamente."
            };

            return Ok(response);
        }

        // ===================== OLVIDÉ MI CONTRASEÑA =====================

        // 1) Usuario escribe su correo -> le enviamos código
        // POST: api/Auth/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("El email es obligatorio.");
            }

            // (Opcional) forzar correo institucional
            // if (!request.Email.EndsWith("@upsjb.edu.pe", StringComparison.OrdinalIgnoreCase))
            // {
            //     return BadRequest("Debes usar tu correo institucional (@upsjb.edu.pe).");
            // }

            var user = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Activo);

            // Por seguridad siempre devolvemos OK, exista o no el usuario
            if (user == null)
            {
                return Ok("Si el correo existe, se enviará un código de recuperación.");
            }

            // Generar código de 6 dígitos
            var code = GenerateResetCode();

            user.PasswordResetCode = code;
            user.PasswordResetCodeExpiration = DateTime.Now.AddMinutes(15); // válido 15 min
            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendPasswordResetEmail(user.Email, code);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al enviar el correo: {ex.Message}");
            }

            return Ok("Se ha enviado un código de verificación al correo institucional.");
        }

        // 2) Usuario escribe código + nueva contraseña
        // POST: api/Auth/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Code) ||
                string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest("Email, código y nueva contraseña son obligatorios.");
            }

            var user = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Activo);

            if (user == null)
            {
                return BadRequest("Usuario no encontrado.");
            }

            if (string.IsNullOrEmpty(user.PasswordResetCode) ||
                user.PasswordResetCodeExpiration == null ||
                user.PasswordResetCodeExpiration < DateTime.Now ||
                !string.Equals(user.PasswordResetCode, request.Code, StringComparison.Ordinal))
            {
                return BadRequest("Código inválido o vencido.");
            }

            // Actualizamos contraseña
            user.PasswordHash = HashPassword(request.NewPassword);
            user.PasswordResetCode = null;
            user.PasswordResetCodeExpiration = null;

            await _context.SaveChangesAsync();

            return Ok("Contraseña actualizada correctamente.");
        }

        // ===================== HELPERS DE CONTRASEÑA =====================

        // Método para encriptar la contraseña al registrarse
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes); // Devuelve la contraseña encriptada en Base64
            }
        }

        // Método para verificar si la contraseña ingresada corresponde con el hash almacenado
        private bool VerifyPasswordHash(string password, string storedHash)
        {
            var hash = HashPassword(password);
            return hash == storedHash; // Compara el hash calculado con el hash almacenado
        }

        private string GenerateResetCode()
        {
            // Código aleatorio de 6 dígitos (ej: 482931)
            var bytes = new byte[4];
            RandomNumberGenerator.Fill(bytes);
            var value = BitConverter.ToInt32(bytes, 0) & int.MaxValue;
            var code = (value % 900000) + 100000;
            return code.ToString();
        }
    }

    // DTOs para recuperación de contraseña
    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
