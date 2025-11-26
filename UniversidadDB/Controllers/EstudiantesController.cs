using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversidadDB.Data;
using UniversidadDB.Dtos;
using UniversidadDB.Helpers;
using UniversidadDB.Models;

namespace UniversidadDB.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "ESTUDIANTE")]
    public class EstudiantesController : ControllerBase
    {
        private readonly UniversidadContext _db;

        public EstudiantesController(UniversidadContext db)
        {
            _db = db;
        }

        // ✅ Helper para obtener el ID del usuario autenticado
        private int GetUserId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (claim == null)
                throw new Exception("No se encontró el claim del usuario.");

            return int.Parse(claim.Value);
        }

        // ==========================================
        // 🔹 GET /api/Estudiantes/me/malla
        // ==========================================
        [HttpGet("me/malla")]
        public async Task<IActionResult> GetMyMalla()
        {
            try
            {
                var userId = AuthHelpers.GetUserId(User);
                var estudianteId = await AuthHelpers.GetEstudianteIdAsync(_db, userId);

                var est = await _db.Estudiantes.AsNoTracking()
                    .Where(e => e.EstudianteId == estudianteId)
                    .Select(e => new { e.CarreraId })
                    .FirstOrDefaultAsync();

                if (est is null)
                    return Unauthorized(new { message = "Estudiante no encontrado." });

                if (est.CarreraId is null)
                    return BadRequest(new { message = "El estudiante no tiene CarreraId asignado." });

                var carreraId = est.CarreraId.Value;

                var carrera = await _db.Carreras.AsNoTracking()
                    .Where(c => c.CarreraId == carreraId)
                    .Select(c => new { c.CarreraId, c.Nombre })
                    .FirstOrDefaultAsync();

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

                var estados = await _db.EstudianteCursoEstados.AsNoTracking()
                    .Where(x => x.EstudianteId == estudianteId && cursoIds.Contains(x.CursoId))
                    .Select(x => new { x.CursoId, x.Estado })
                    .ToListAsync();

                var estadoMap = estados.ToDictionary(x => x.CursoId, x => x.Estado);

                var prereqs = await _db.Prerequisitos.AsNoTracking()
                    .Where(p => cursoIds.Contains(p.CursoId))
                    .Select(p => new { p.CursoId, p.CursoPrereqId })
                    .ToListAsync();

                var prereqMap = prereqs
                    .GroupBy(p => p.CursoId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.CursoPrereqId).Distinct().ToList());

                var cursos = malla.Select(x =>
                {
                    var estActual = estadoMap.TryGetValue(x.CursoId, out var st) ? st : "PENDIENTE";

                    prereqMap.TryGetValue(x.CursoId, out var reqs);
                    reqs ??= new List<int>();

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
                return Unauthorized(new { message = ex.Message });
            }
        }

        // ==========================================
        // 🔹 PUT /api/Estudiantes/me/malla/{cursoId}/estado
        // ==========================================
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

                var carreraId = await _db.Estudiantes.AsNoTracking()
                    .Where(e => e.EstudianteId == estudianteId)
                    .Select(e => e.CarreraId)
                    .FirstOrDefaultAsync();

                if (carreraId is null)
                    return BadRequest(new { message = "El estudiante no tiene CarreraId asignado." });

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

        // ==========================================
        // 🔹 GET /api/Estudiantes/me/malla/{cursoId}/notas
        // ==========================================
        [HttpGet("me/malla/{cursoId}/notas")]
        public async Task<IActionResult> GetNotasCurso(int cursoId)
        {
            var userId = GetUserId();

            var notas = await _db.Notas
                .Where(n => n.EstudianteId == userId && n.CursoId == cursoId)
                .Select(n => new { n.NotaId, n.Nombre, n.NotaValor, n.FechaRegistro })
                .ToListAsync();

            return Ok(notas);
        }

        // ==========================================
        // 🔹 POST /api/Estudiantes/me/malla/{cursoId}/notas
        // ==========================================
        [HttpPost("me/malla/{cursoId}/notas")]
        public async Task<IActionResult> AddNotaCurso(int cursoId, [FromBody] NotaDto dto)
        {
            var userId = GetUserId();

            var nota = new Nota
            {
                EstudianteId = userId,
                CursoId = cursoId,
                Nombre = dto.Nombre,
                NotaValor = dto.Nota,
                FechaRegistro = DateTime.UtcNow
            };

            _db.Notas.Add(nota);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                nota.NotaId,
                nota.Nombre,
                nota.NotaValor,
                nota.FechaRegistro
            });
        }

        // DTO interno para recibir datos
        public class NotaDto
        {
            public string Nombre { get; set; } = string.Empty;
            public double Nota { get; set; }
        }
    }
}
