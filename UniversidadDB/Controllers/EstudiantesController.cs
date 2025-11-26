using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
public class EstudiantesController : ControllerBase
{
    private readonly UniversidadContext _db;
    public EstudiantesController(UniversidadContext db) => _db = db;

    // GET /api/Estudiantes/me/malla
    [HttpGet("me/malla")]
    public async Task<IActionResult> GetMyMalla()
    {
        try
        {
            var userId = AuthHelpers.GetUserId(User);
            var estudianteId = await AuthHelpers.GetEstudianteIdAsync(_db, userId);

            // 1) Obtener CarreraId del estudiante
            var est = await _db.Estudiantes.AsNoTracking()
                .Where(e => e.EstudianteId == estudianteId)
                .Select(e => new { e.CarreraId })
                .FirstOrDefaultAsync();

            if (est is null) return Unauthorized(new { message = "Estudiante no encontrado." });
            if (est.CarreraId is null) return BadRequest(new { message = "El estudiante no tiene CarreraId asignado." });

            var carreraId = est.CarreraId.Value;

            // 2) Datos de carrera
            var carrera = await _db.Carreras.AsNoTracking()
                .Where(c => c.CarreraId == carreraId)
                .Select(c => new { c.CarreraId, c.Nombre })
                .FirstOrDefaultAsync();

            // 3) Malla base de la carrera
            var malla = await (
                from m in _db.MallaCarrera.AsNoTracking()
                join c in _db.Cursos.AsNoTracking() on m.CursoId equals c.CursoId
                where m.CarreraId == carreraId && m.Activo
                select new
                {
                    m.CursoId,
                    CursoNombre = c.Nombre,
                    m.Ciclo,
                    m.Creditos,
                    m.Obligatorio
                }
            )
            .OrderBy(x => x.Ciclo)
            .ThenBy(x => x.CursoNombre)
            .ToListAsync();

            var cursoIds = malla.Select(x => x.CursoId).Distinct().ToList();

            // 4) Estados del estudiante (solo para esos cursos)
            var estados = await _db.EstudianteCursoEstados.AsNoTracking()
                .Where(x => x.EstudianteId == estudianteId && cursoIds.Contains(x.CursoId))
                .Select(x => new { x.CursoId, x.Estado })
                .ToListAsync();

            var estadoMap = estados.ToDictionary(x => x.CursoId, x => x.Estado);

            // 5) Prerrequisitos de esos cursos
            var prereqs = await _db.Prerequisitos.AsNoTracking()
                .Where(p => cursoIds.Contains(p.CursoId))
                .Select(p => new { p.CursoId, p.CursoPrereqId })
                .ToListAsync();

            var prereqMap = prereqs
                .GroupBy(p => p.CursoId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.CursoPrereqId).Distinct().ToList());

            // 6) Respuesta final con lock 🔒
            var cursos = malla.Select(x =>
            {
                var estActual = estadoMap.TryGetValue(x.CursoId, out var st) ? st : "PENDIENTE";

                prereqMap.TryGetValue(x.CursoId, out var reqs);
                reqs ??= new List<int>();

                // si ya está aprobado, no lo bloquees
                var locked = estActual != "APROBADO" &&
                             reqs.Any(r => !(estadoMap.TryGetValue(r, out var stReq) && stReq == "APROBADO"));

                var faltantes = reqs
                    .Where(r => !(estadoMap.TryGetValue(r, out var stReq) && stReq == "APROBADO"))
                    .ToList();

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

            return Ok(new { carrera, cursos });
        }
        catch (UnauthorizedAccessException ex)
        {
            // ⚠️ NO uses Forbid("mensaje") porque lo toma como "scheme" y revienta
            return Unauthorized(new { message = ex.Message });
        }
    }

    // PUT /api/Estudiantes/me/malla/{cursoId}/estado
    [HttpPut("me/malla/{cursoId:int}/estado")]
    public async Task<IActionResult> UpdateEstado(int cursoId, [FromBody] EstadoCursoUpdateDto dto)
    {
        try
        {
            var allowed = new HashSet<string> { "PENDIENTE", "EN_CURSO", "APROBADO", "REPROBADO" };
            var estado = (dto.Estado ?? "").Trim().ToUpperInvariant();
            if (!allowed.Contains(estado))
                return BadRequest(new { message = "Estado inválido." });

            var userId = AuthHelpers.GetUserId(User);
            var estudianteId = await AuthHelpers.GetEstudianteIdAsync(_db, userId);

            // CarreraId del estudiante
            var carreraId = await _db.Estudiantes.AsNoTracking()
                .Where(e => e.EstudianteId == estudianteId)
                .Select(e => e.CarreraId)
                .FirstOrDefaultAsync();

            if (carreraId is null)
                return BadRequest(new { message = "El estudiante no tiene CarreraId asignado." });

            // Validar que el curso exista en la malla de SU carrera
            var enMalla = await _db.MallaCarrera.AsNoTracking()
                .AnyAsync(m => m.CarreraId == carreraId.Value && m.CursoId == cursoId && m.Activo);

            if (!enMalla)
                return NotFound(new { message = "Curso no pertenece a tu malla." });

            var row = await _db.EstudianteCursoEstados
                .FirstOrDefaultAsync(x => x.EstudianteId == estudianteId && x.CursoId == cursoId);

            if (row == null)
            {
                row = new EstudianteCursoEstado
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

            return Ok(new
            {
                row.EstudianteId,
                row.CursoId,
                row.Estado,
                row.PeriodoId,
                row.NotaFinal,
                row.UpdatedAt
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}
