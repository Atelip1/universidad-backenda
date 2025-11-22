using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversidadDB.Data;
using UniversidadDB.Models;
using UniversidadDB.Models.Auth;
using System;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;

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
                CodigoEstudiante = user.Estudiante?.CodigoEstudiante, // 👈 ajusta al nombre real de tu propiedad
                Ciclo = user.Estudiante?.Ciclo,
                Message = "Login correcto."
            };

            return Ok(response);
        }

        // POST: api/Auth/register-estudiante
        [HttpPost("register-estudiante")]
        public async Task<ActionResult<LoginResponse>> RegisterEstudiante([FromBody] RegisterEstudianteRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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
                // Llamamos al servicio de correo para enviar el email
                await _emailService.SendWelcomeEmail(request.Email, request.NombreCompleto);
            }
            catch (Exception ex)
            {
                // Si ocurre algún error al enviar el correo, se captura y responde con un mensaje adecuado
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
    }
}
