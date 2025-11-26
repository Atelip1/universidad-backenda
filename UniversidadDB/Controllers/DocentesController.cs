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

        // Si quieres devolver la foto aquí también, se debe hacer después de guardar el docente.
        return CreatedAtAction(nameof(GetDocente), new { id = docente.Id }, docente);
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
        if (id != docente.Id)
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

        // Opción: Desactivar docente en lugar de eliminar
        docente.Activo = false;  // 'Activo' es un campo booleano en tu modelo de Docente
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

        // Verificar si el archivo es válido (por ejemplo, tipo de archivo, tamaño, etc.)
        if (foto == null || foto.Length == 0)
        {
            return BadRequest("No se ha enviado una foto válida.");
        }

        var fotoUrl = await SubirImagen(foto); // Método para subir la foto y obtener la URL

        docente.FotoUrl = fotoUrl; // Guardar la URL de la foto
        await _context.SaveChangesAsync();

        return Ok(new { message = "Foto subida correctamente", fotoUrl });
    }

    // Método para simular la subida de imagen
    private async Task<string> SubirImagen(IFormFile foto)
    {
        // Aquí puedes implementar la lógica real para subir la foto a un servidor de almacenamiento
        // o utilizar un servicio de almacenamiento en la nube como AWS S3 o Azure Blob Storage.

        // Para este ejemplo, solo guardamos el archivo en el servidor local:
        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsPath);

        var filePath = Path.Combine(uploadsPath, foto.FileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await foto.CopyToAsync(stream);
        }

        return $"/uploads/{foto.FileName}";  // Esta es la URL de acceso a la foto
    }

    // LIST: Obtener lista de docentes (sin CRUD para Estudiante)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Docente>>> GetDocentes([FromQuery] string busqueda = "")
    {
        var docentes = _context.Docentes.AsQueryable();

        if (!string.IsNullOrEmpty(busqueda))
        {
            docentes = docentes.Where(d => d.Nombre.ToLower().Contains(busqueda.ToLower()) || d.Especialidad.ToLower().Contains(busqueda.ToLower()));
        }

        return await docentes.ToListAsync();
    }

    // READ: Ver detalle del docente
    [HttpGet("detalle/{id}")]
    public async Task<ActionResult<Docente>> GetDocenteDetalle(int id)
    {
        var docente = await _context.Docentes.FindAsync(id);

        if (docente == null)
        {
            return NotFound();
        }

        return docente;
    }
}
