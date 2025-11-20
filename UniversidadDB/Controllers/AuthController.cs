using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversidadDB.Data;
using UniversidadDB.Models;
using UniversidadDB.Models.Auth;

namespace UniversidadDB.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UniversidadContext _context;

        public AuthController(UniversidadContext context)
        {
            _context = context;
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

            // ⚠️ Simple: comparar contraseña en texto plano
            if (user.PasswordHash != request.Password)
            {
                return Unauthorized("Contraseña incorrecta.");
            }

            var response = new LoginResponse
            {
                UsuarioId = user.UsuarioId,
                NombreCompleto = user.NombreCompleto,
                Email = user.Email,
                Rol = user.Rol.NombreRol,                // "ADMIN" o "ESTUDIANTE"
                EstudianteId = user.Estudiante?.EstudianteId,
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

            // 1. verificar que no exista ya el email
            var existing = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existing != null)
            {
                return Conflict("Ya existe un usuario con ese email.");
            }

            // 2. obtener Rol ESTUDIANTE
            var rolEstudiante = await _context.Roles.FirstOrDefaultAsync(r => r.NombreRol == "ESTUDIANTE");
            if (rolEstudiante == null)
            {
                return StatusCode(500, "No se encuentra el rol ESTUDIANTE en la base de datos.");
            }

            // 3. crear Usuario
            var usuario = new Usuario
            {
                NombreCompleto = request.NombreCompleto,
                Email = request.Email,
                PasswordHash = request.Password,   // ⚠️ en la vida real: hashear
                RolId = rolEstudiante.RolId,
                Activo = true,
                FechaRegistro = DateTime.Now
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync(); // guarda para tener UsuarioId

            // 4. crear Estudiante (1:1 con Usuario)
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
    }
}