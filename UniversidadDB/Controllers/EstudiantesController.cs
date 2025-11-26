using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversidadDB.Data;
using UniversidadDB.Dtos;
using UniversidadDB.Helpers;

namespace UniversidadDB.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ESTUDIANTE")]
public class EstudiantesController : ControllerBase
{
    private readonly UniversidadContext _db;
    public EstudiantesController(UniversidadContext db) => _db = db;

    // GET /api/Estudiantes/me/malla
    [HttpGet("me/malla")]
    public async Task<IActionResult> GetMyMalla()
    {
        var userId = AuthHelpers.GetUserId(User);
        var estudianteId = await AuthHelpers.GetEstudianteIdAsync(_db, userId);

        var est = await _db.Estudiantes.AsNoTracking()
            .Where(e => e.EstudianteId == estudianteId)
            .Select(e => new { e.CarreraId })
            .FirstOrDefaultAsync();

        if (est == null) return Unauthorized();
        if (est.CarreraId == null) return BadRequest("El estudiante no tiene CarreraId asignado.");

        var carrera = await _db.Carreras.AsNoTracking()
            .Where(c => c.CarreraId == est.CarreraId.Value)
            .Select(c => new { c.CarreraId, c.Nombre })
            .FirstOrDefaultAsync();

        // malla base
        var malla = await (from m in _db.MallaCarrera.AsNoTracking()
                           join c in _db.Cursos.AsNoTracking() on m.CursoId equals c.CursoId
                           where m.CarreraId == est.CarreraId.Value && m.Activo
                           select new
                           {
                               m.CursoId,
                               CursoNombre = c.Nombre,
                               m.Ciclo,
                               m.Creditos,
                               m.Obligatorio
                           })
                           .OrderBy(x => x.Ciclo).ThenBy(x => x.CursoNombre)
                           .ToListAsync();

        var cursoIds = malla.Select(x => x.CursoId).ToList();

        // estados del estudiante
        var estados = await _db.EstudianteCursoEstados.AsNoTracking()
            .Where(x => x.EstudianteId == estudianteId && cursoIds.Contains(x.CursoId))
            .ToListAsync();

        var estadoMap = estados.ToDictionary(x => x.CursoId, x => x.Estado);

        // prerequisitos de esos cursos
        var prereqs = await _db.Prerequisitos.AsNoTracking()
            .Where(p => cursoIds.Contains(p.CursoId))
            .ToListAsync();

        var prereqMap = prereqs
            .GroupBy(p => p.CursoId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.CursoPrereqId).Distinct().ToList());

        // respuesta con lógica de bloqueado (🔒) por prerequisitos no aprobados
        var cursos = malla.Select(x =>
        {
            var estActual = estadoMap.TryGetValue(x.CursoId, out var st) ? st : "PENDIENTE";
            prereqMap.TryGetValue(x.CursoId, out var reqs);
            reqs ??= new List<int>();

            // si ya está aprobado, no lo bloquees
            var locked = estActual != "APROBADO" &&
                         reqs.Any(r => !(estadoMap.TryGetValue(r, out var stReq) && stReq == "APROBADO"));

            var faltantes = reqs.Where(r => !(estadoMap.TryGetValue(r, out var stReq) && stReq == "APROBADO")).ToList();

            return new
            {
                x.CursoId,
                x.CursoNombre,
                x.Ciclo,
                x.Creditos,
                x.Obligatorio,
                Estado = estActual,
                IsLocked = locked,
                Prerequisitos = reqs,
                PrerequisitosPendientes = faltantes
            };
        }).ToList();

        return Ok(new
        {
            carrera,
            cursos
        });
    }

    // PUT /api/Estudiantes/me/malla/{cursoId}/estado
    [HttpPut("me/malla/{cursoId:int}/estado")]
    public async Task<IActionResult> UpdateEstado(int cursoId, [FromBody] EstadoCursoUpdateDto dto)
    {
        var allowed = new HashSet<string> { "PENDIENTE", "EN_CURSO", "APROBADO", "REPROBADO" };
        var estado = (dto.Estado ?? "").Trim().ToUpperInvariant();
        if (!allowed.Contains(estado)) return BadRequest("Estado inválido.");

        var userId = AuthHelpers.GetUserId(User);
        var estudianteId = await AuthHelpers.GetEstudianteIdAsync(_db, userId);

        var row = await _db.EstudianteCursoEstados
            .FirstOrDefaultAsync(x => x.EstudianteId == estudianteId && x.CursoId == cursoId);

        if (row == null)
        {
            row = new UniversidadDB.Models.EstudianteCursoEstado
            {
                EstudianteId = estudianteId,
                CursoId = cursoId
            };
            _db.EstudianteCursoEstados.Add(row);
        }

        row.Estado = estado;
        row.PeriodoId = dto.PeriodoId;
        row.NotaFinal = dto.NotaFinal;
        row.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { row.EstudianteId, row.CursoId, row.Estado, row.PeriodoId, row.NotaFinal, row.UpdatedAt });
    }
}
