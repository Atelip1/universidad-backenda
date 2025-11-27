using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversidadDB.Data;
using UniversidadDB.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class DocentesController : ControllerBase
{
    private readonly UniversidadContext _context;

    public DocentesController(UniversidadContext context)
    {
        _context = context;
    }

    // CREATE: Crear docente
    [HttpPost]
    public async Task<IActionResult> CrearDocente([FromBody] Docente docente)
    {
        if (docente == null)
        {
            return BadRequest("Datos inválidos.");
        }

        _context.Docentes.Add(docente);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDocente), new { id = docente.DocenteId }, docente);
    }

    // READ: Ver docente por Id
    [HttpGet("{id}")]
    public async Task<ActionResult<Docente>> GetDocente(int id)
    {
        var docente = await _context.Docentes.FindAsync(id);

        if (docente == null)
        {
            return NotFound();
        }

        return docente;
    }

    // UPDATE: Editar docente
    [HttpPut("{id}")]
    public async Task<IActionResult> EditarDocente(int id, [FromBody] Docente docente)
    {
        if (id != docente.DocenteId)
        {
            return BadRequest("El ID del docente no coincide.");
        }

        _context.Entry(docente).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE (Opción desactivar docente)
    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarDocente(int id)
    {
        var docente = await _context.Docentes.FindAsync(id);
        if (docente == null)
        {
            return NotFound();
        }

        docente.IsActive = false;  // 'IsActive' en lugar de 'Activo'
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Subir foto (Guardar la URL de la foto)
    [HttpPost("{id}/foto")]
    public async Task<IActionResult> SubirFoto(int id, [FromForm] IFormFile foto)
    {
        var docente = await _context.Docentes.FindAsync(id);
        if (docente == null)
        {
            return NotFound();
        }

        if (foto == null || foto.Length == 0)
        {
            return BadRequest("No se ha enviado una foto válida.");
        }

        var fotoUrl = await SubirImagen(foto);

        docente.FotoUrl = fotoUrl;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Foto subida correctamente", fotoUrl });
    }

    private async Task<string> SubirImagen(IFormFile foto)
    {
        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsPath);

        var filePath = Path.Combine(uploadsPath, foto.FileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await foto.CopyToAsync(stream);
        }

        return $"/uploads/{foto.FileName}";
    }

    // LIST: Obtener lista de docentes
 
        // Obtener la lista completa de docentes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Docente>>> GetDocentes()
        {
            // Obtener todos los docentes activos y ordenarlos por apellido y nombre
            var docentes = await _context.Docentes
                .AsNoTracking()   // Para evitar cambios en la base de datos
                .Where(d => d.IsActive)  // Solo los docentes activos
                .OrderBy(d => d.Apellidos)  // Ordenar por apellido primero
                .ThenBy(d => d.Nombres)    // Luego por nombre
                .ToListAsync();

            return Ok(docentes);
        }

        // Obtener detalle de un docente
        [HttpGet("detalle/{id:int}")]
        public async Task<ActionResult<Docente>> GetDocenteDetalle(int id)
        {
            // Buscar al docente por su ID
            var docente = await _context.Docentes.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DocenteId == id && d.IsActive);  // Solo activos

            if (docente == null) return NotFound();
            return Ok(docente);
        }
}
