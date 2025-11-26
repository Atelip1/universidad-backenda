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
[Authorize]
public class AgendaController : ControllerBase
{
    private readonly UniversidadContext _db;
    public AgendaController(UniversidadContext db) => _db = db;

    // GET /api/agenda/me?from=2025-11-01&to=2025-11-30&completed=false
    [HttpGet("me")]
    public async Task<IActionResult> MyAgenda(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] bool? completed)
    {
        var userId = AuthHelpers.GetUserId(User);
        var estudianteId = await AuthHelpers.GetEstudianteIdAsync(_db, userId);

        // Defaults si no envías fechas
        var fromVal = from ?? DateTime.UtcNow.AddDays(-30);
        var toVal = to ?? DateTime.UtcNow.AddDays(30);

        var q = _db.AgendaEventos.AsNoTracking()
            .Where(e => e.EstudianteId == estudianteId && e.StartAt < toVal && e.EndAt > fromVal)
            .AsQueryable();

        if (completed.HasValue) q = q.Where(e => e.IsCompleted == completed.Value);

        var data = await q.OrderBy(e => e.StartAt).ToListAsync();
        return Ok(data);
    }

    // POST /api/agenda/me
    [HttpPost("me")]
    public async Task<IActionResult> Create([FromBody] AgendaCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Titulo)) return BadRequest("Título requerido.");
        if (dto.EndAt <= dto.StartAt) return BadRequest("EndAt debe ser mayor que StartAt.");
        if (dto.ReminderMinutesBefore is < 0) return BadRequest("ReminderMinutesBefore inválido.");

        var userId = AuthHelpers.GetUserId(User);
        var estudianteId = await AuthHelpers.GetEstudianteIdAsync(_db, userId);

        var now = DateTime.UtcNow;

        var ev = new AgendaEvento
        {
            EstudianteId = estudianteId,
            CursoId = dto.CursoId,
            Titulo = dto.Titulo.Trim(),
            Nota = dto.Nota,
            StartAt = dto.StartAt,
            EndAt = dto.EndAt,
            RepeatRule = dto.RepeatRule,
            ReminderMinutesBefore = dto.ReminderMinutesBefore,
            IsCompleted = false,
            CompletedAt = null,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.AgendaEventos.Add(ev);
        await _db.SaveChangesAsync();
        return Ok(ev);
    }

    // PUT /api/agenda/me/{eventoId}
    [HttpPut("me/{eventoId:int}")]
    public async Task<IActionResult> Update(int eventoId, [FromBody] AgendaUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Titulo)) return BadRequest("Título requerido.");
        if (dto.EndAt <= dto.StartAt) return BadRequest("EndAt debe ser mayor que StartAt.");

        var userId = AuthHelpers.GetUserId(User);
        var estudianteId = await AuthHelpers.GetEstudianteIdAsync(_db, userId);

        var ev = await _db.AgendaEventos.FirstOrDefaultAsync(x => x.EventoId == eventoId && x.EstudianteId == estudianteId);
        if (ev is null) return NotFound();

        ev.CursoId = dto.CursoId;
        ev.Titulo = dto.Titulo.Trim();
        ev.Nota = dto.Nota;
        ev.StartAt = dto.StartAt;
        ev.EndAt = dto.EndAt;
        ev.RepeatRule = dto.RepeatRule;
        ev.ReminderMinutesBefore = dto.ReminderMinutesBefore;

        ev.IsCompleted = dto.IsCompleted;
        ev.CompletedAt = dto.IsCompleted ? (ev.CompletedAt ?? DateTime.UtcNow) : null;

        ev.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ev);
    }

    // PUT /api/agenda/me/{eventoId}/completar
    [HttpPut("me/{eventoId:int}/completar")]
    public async Task<IActionResult> Complete(int eventoId)
    {
        var userId = AuthHelpers.GetUserId(User);
        var estudianteId = await AuthHelpers.GetEstudianteIdAsync(_db, userId);

        var ev = await _db.AgendaEventos.FirstOrDefaultAsync(x => x.EventoId == eventoId && x.EstudianteId == estudianteId);
        if (ev is null) return NotFound();

        ev.IsCompleted = true;
        ev.CompletedAt = DateTime.UtcNow;
        ev.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { ev.EventoId, ev.IsCompleted, ev.CompletedAt });
    }

    // DELETE /api/agenda/me/{eventoId}
    [HttpDelete("me/{eventoId:int}")]
    public async Task<IActionResult> Delete(int eventoId)
    {
        var userId = AuthHelpers.GetUserId(User);
        var estudianteId = await AuthHelpers.GetEstudianteIdAsync(_db, userId);

        var ev = await _db.AgendaEventos.FirstOrDefaultAsync(x => x.EventoId == eventoId && x.EstudianteId == estudianteId);
        if (ev is null) return NotFound();

        _db.AgendaEventos.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
