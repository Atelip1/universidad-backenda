using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversidadDB.Data;
using UniversidadDB.Models;

namespace UniversidadDB.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CursosController : ControllerBase
    {
        private readonly UniversidadContext _context;

        public CursosController(UniversidadContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Curso>>> Get()
        {
            var cursos = await _context.Cursos.ToListAsync();
            return Ok(cursos);
        }
        [HttpPost("{cursoId}/materiales/colaborativo")]
        [Authorize(Roles = "ESTUDIANTE")]
        public async Task<IActionResult> SubirMaterialColaborativo(int cursoId, IFormFile file, [FromForm] bool visibleTodos)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No se envió ningún archivo." });

            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { message = "Usuario no autorizado." });

            // Carpeta donde se guardarán los archivos
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Materiales");
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            // Nombre único
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // Guardar archivo
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Guardar registro en BD (si tienes una tabla MaterialCurso)
            var material = new MaterialCurso
            {
                CursoId = cursoId,
                Nombre = file.FileName,
                Ruta = $"/Uploads/Materiales/{fileName}",
                FechaSubida = DateTime.UtcNow,
                SubidoPor = int.Parse(userId),
                VisibleParaTodos = visibleTodos
            };

            _context.MaterialCursos.Add(material);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                material.MaterialId,
                material.Nombre,
                material.VisibleParaTodos,
                material.FechaSubida
            });
        }

    }

}