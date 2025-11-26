using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversidadDB.Data;
using UniversidadDB.Dtos;
using UniversidadDB.Helpers;
using UniversidadDB.Models;

namespace UniversidadDB.Controllers;

[ApiController]
[Route("api/Cursos/{cursoId:int}/materiales")]
[Authorize] // ambos roles pueden leer
public class CursosMaterialesController : ControllerBase
{
    private readonly UniversidadContext _db;
    public CursosMaterialesController(UniversidadContext db) => _db = db;

    // GET /api/Cursos/{cursoId}/materiales/oficial
    [HttpGet("oficial")]
    public async Task<IActionResult> GetOficial(int cursoId)
    {
        var data = await _db.CursoMateriales.AsNoTracking()
            .Where(x => x.CursoId == cursoId && !x.IsDeleted && x.Source == "ADMIN" && x.Status == "APPROVED")
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(data);
    }

    // GET /api/Cursos/{cursoId}/materiales/colaborativo
    [HttpGet("colaborativo")]
    public async Task<IActionResult> GetColaborativoAprobado(int cursoId)
    {
        var data = await _db.CursoMateriales.AsNoTracking()
            .Where(x => x.CursoId == cursoId && !x.IsDeleted && x.Source == "STUDENT" && x.Status == "APPROVED")
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(data);
    }

    // POST /api/Cursos/{cursoId}/materiales/colaborativo  (ESTUDIANTE propone -> PENDING)
    [HttpPost("colaborativo")]
    [Authorize(Roles = "ESTUDIANTE")]
    public async Task<IActionResult> ProponerColaborativo(int cursoId, [FromBody] MaterialCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Titulo)) return BadRequest("Título requerido.");
        if (string.IsNullOrWhiteSpace(dto.Url)) return BadRequest("URL requerida.");

        var userId = AuthHelpers.GetUserId(User);
        var _ = await AuthHelpers.GetEstudianteIdAsync(_db, userId); // valida que sea estudiante

        var now = DateTime.UtcNow;

        var mat = new CursoMaterial
        {
            CursoId = cursoId,
            AutorUsuarioId = userId,
            Source = "STUDENT",
            Status = "PENDING",
            Titulo = dto.Titulo.Trim(),
            Url = dto.Url.Trim(),
            Descripcion = dto.Descripcion,
            FileName = dto.FileName,
            MimeType = dto.MimeType,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };

        _db.CursoMateriales.Add(mat);
        await _db.SaveChangesAsync();
        return Ok(mat);
    }

    // POST /api/Cursos/{cursoId}/materiales/oficial (ADMIN publica directo)
    [HttpPost("oficial")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> CreateOficial(int cursoId, [FromBody] MaterialCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Titulo)) return BadRequest("Título requerido.");
        if (string.IsNullOrWhiteSpace(dto.Url)) return BadRequest("URL requerida.");

        var userId = AuthHelpers.GetUserId(User);
        var now = DateTime.UtcNow;

        var mat = new CursoMaterial
        {
            CursoId = cursoId,
            AutorUsuarioId = userId,
            ApprovedByUsuarioId = userId,
            Source = "ADMIN",
            Status = "APPROVED",
            Titulo = dto.Titulo.Trim(),
            Url = dto.Url.Trim(),
            Descripcion = dto.Descripcion,
            FileName = dto.FileName,
            MimeType = dto.MimeType,
            CreatedAt = now,
            UpdatedAt = now,
            ApprovedAt = now,
            IsDeleted = false
        };

        _db.CursoMateriales.Add(mat);
        await _db.SaveChangesAsync();
        return Ok(mat);
    }
}
