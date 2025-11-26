using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversidadDB.Data;
using UniversidadDB.Dtos;
using UniversidadDB.Helpers;
using UniversidadDB.Models;

namespace UniversidadDB.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ESTUDIANTE")]

public class ApuntesController : ControllerBase
{
    private readonly UniversidadContext _db;
    public ApuntesController(UniversidadContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List(
    [FromQuery] string? search,
    [FromQuery] bool? pinned,
    [FromQuery] int? cursoId)
    {
        try
        {
            var userId = AuthHelpers.GetUserId(User);
            if (userId <= 0) return Unauthorized(new { message = "JWT inválido." });

            int estudianteId;
            try
            {
                estudianteId = await AuthHelpers.GetEstudianteIdAsync(_db, userId);
            }
            catch (UnauthorizedAccessException)
            {
                // ✅ NO uses Forbid(ex.Message)
                return StatusCode(403, new { message = "Este usuario no está vinculado a un estudiante." });
            }

            var q = _db.Apuntes.AsNoTracking()
                .Where(a => a.EstudianteId == estudianteId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(a => a.Titulo.Contains(s) || a.Contenido.Contains(s));
            }

            if (pinned.HasValue) q = q.Where(a => a.IsPinned == pinned.Value);
            if (cursoId.HasValue) q = q.Where(a => a.CursoId == cursoId.Value);

            var data = await q.OrderByDescending(a => a.IsPinned)
                              .ThenByDescending(a => a.UpdatedAt)
                              .ToListAsync();

            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error en Apuntes: {ex.Message}" });
        }
    }


    [HttpGet("{apunteId:int}")]
    public async Task<IActionResult> GetOne(int apunteId)
    {
        var userId = AuthHelpers.GetUserId(User);
        var estudianteId = await AuthHelpers.GetEstudianteIdAsync(_db, userId);

        var apunte = await _db.Apuntes.AsNoTracking()
            .Include(a => a.Adjuntos)
            .FirstOrDefaultAsync(a => a.ApunteId == apunteId && a.EstudianteId == estudianteId);

        return apunte is null ? NotFound() : Ok(apunte);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ApunteCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Titulo)) return BadRequest("Título requerido.");
        if (string.IsNullOrWhiteSpace(dto.Contenido)) return BadRequest("Contenido requerido.");

        var userId = AuthHelpers.GetUserId(User);
        var estudianteId = await AuthHelpers.GetEstudianteIdAsync(_db, userId);

        var now = DateTime.UtcNow;

        var apunte = new Apunte
        {
            EstudianteId = estudianteId,
            CursoId = dto.CursoId,
            Titulo = dto.Titulo.Trim(),
            Contenido = dto.Contenido,
            IsPinned = dto.IsPinned,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Apuntes.Add(apunte);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOne), new { apunteId = apunte.ApunteId }, apunte);
    }

    [HttpPut("{apunteId:int}")]
    public async Task<IActionResult> Update(int apunteId, [FromBody] ApunteUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Titulo)) return BadRequest("Título requerido.");
        if (string.IsNullOrWhiteSpace(dto.Contenido)) return BadRequest("Contenido requerido.");

        var userId = AuthHelpers.GetUserId(User);
        var estudianteId = await AuthHelpers.GetEstudianteIdAsync(_db, userId);

        var apunte = await _db.Apuntes.FirstOrDefaultAsync(a => a.ApunteId == apunteId && a.EstudianteId == estudianteId);
        if (apunte is null) return NotFound();

        apunte.CursoId = dto.CursoId;
        apunte.Titulo = dto.Titulo.Trim();
        apunte.Contenido = dto.Contenido;
        apunte.IsPinned = dto.IsPinned;
        apunte.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(apunte);
    }

    [HttpPut("{apunteId:int}/pin")]
    public async Task<IActionResult> TogglePin(int apunteId)
    {
        var userId = AuthHelpers.GetUserId(User);
        var estudianteId = await AuthHelpers.GetEstudianteIdAsync(_db, userId);

        var apunte = await _db.Apuntes.FirstOrDefaultAsync(a => a.ApunteId == apunteId && a.EstudianteId == estudianteId);
        if (apunte is null) return NotFound();

        apunte.IsPinned = !apunte.IsPinned;
        apunte.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { apunte.ApunteId, apunte.IsPinned });
    }

    [HttpDelete("{apunteId:int}")]
    public async Task<IActionResult> Delete(int apunteId)
    {
        var userId = AuthHelpers.GetUserId(User);
        var estudianteId = await AuthHelpers.GetEstudianteIdAsync(_db, userId);

        var apunte = await _db.Apuntes.FirstOrDefaultAsync(a => a.ApunteId == apunteId && a.EstudianteId == estudianteId);
        if (apunte is null) return NotFound();

        _db.Apuntes.Remove(apunte);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{apunteId:int}/adjuntos")]
    public async Task<IActionResult> AddAdjunto(int apunteId, [FromBody] ApunteAdjuntoCreateDto dto)
    {
        var tipo = (dto.Tipo ?? "").Trim().ToUpperInvariant();
        if (tipo is not ("LINK" or "PDF" or "IMG")) return BadRequest("Tipo inválido: LINK/PDF/IMG.");
        if (string.IsNullOrWhiteSpace(dto.Url)) return BadRequest("Url requerida.");

        var userId = AuthHelpers.GetUserId(User);
        var estudianteId = await AuthHelpers.GetEstudianteIdAsync(_db, userId);

        var exists = await _db.Apuntes.AsNoTracking()
            .AnyAsync(a => a.ApunteId == apunteId && a.EstudianteId == estudianteId);

        if (!exists) return NotFound();

        var adj = new ApunteAdjunto
        {
            ApunteId = apunteId,
            Tipo = tipo,
            Url = dto.Url.Trim(),
            FileName = dto.FileName,
            MimeType = dto.MimeType,
            CreatedAt = DateTime.UtcNow
        };

        _db.ApunteAdjuntos.Add(adj);
        await _db.SaveChangesAsync();
        return Ok(adj);
    }

    [HttpDelete("adjuntos/{adjuntoId:int}")]
    public async Task<IActionResult> DeleteAdjunto(int adjuntoId)
    {
        var userId = AuthHelpers.GetUserId(User);
        var estudianteId = await AuthHelpers.GetEstudianteIdAsync(_db, userId);

        var adj = await _db.ApunteAdjuntos.FirstOrDefaultAsync(x => x.AdjuntoId == adjuntoId);
        if (adj is null) return NotFound();

        var isOwner = await _db.Apuntes.AsNoTracking()
            .AnyAsync(a => a.ApunteId == adj.ApunteId && a.EstudianteId == estudianteId);

        if (!isOwner) return Forbid();

        _db.ApunteAdjuntos.Remove(adj);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
