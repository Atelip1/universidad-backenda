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
                var allowed = new HashSet<string> { "PENDIENTE", "EN_CURSO", "APROBADO", "DESAPROBADO", "REPROBADO" };
                var estado = (dto.Estado ?? "").Trim().ToUpperInvariant();
                if (!allowed.Contains(estado))
                    return BadRequest(new { message = "Estado inválido." });

                var userId = AuthHelpers.GetUserId(User);
                var estudianteId = await AuthHelpers.GetEstudianteIdAsync(_db, userId);

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
                row.NotaFinal = dto.NotaFinal;
                row.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    row.EstudianteId,
                    row.CursoId,
                    row.Estado,
                    row.NotaFinal,
                    row.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
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

            // ✅ Permitir Parcial y Final
            var permitidos = new[] { "PC1", "PC2", "PC3", "PC4", "PARCIAL", "FINAL" };
            var nombre = (dto.Nombre ?? "").Trim().ToUpperInvariant();
            if (!permitidos.Contains(nombre))
                return BadRequest(new { message = "Tipo de nota no válido." });

            // ✅ Si ya existe la nota, actualízala en vez de duplicar
            var notaExistente = await _db.Notas.FirstOrDefaultAsync(n =>
                n.EstudianteId == userId && n.CursoId == cursoId && n.Nombre.ToUpper() == nombre);

            if (notaExistente != null)
            {
                notaExistente.NotaValor = dto.Nota;
                notaExistente.FechaRegistro = DateTime.UtcNow;
            }
            else
            {
                var nueva = new Nota
                {
                    EstudianteId = userId,
                    CursoId = cursoId,
                    Nombre = nombre,
                    NotaValor = dto.Nota,
                    FechaRegistro = DateTime.UtcNow
                };
                _db.Notas.Add(nueva);
            }

            await _db.SaveChangesAsync();

            // ✅ Calcular promedio y actualizar estado
            await CalcularPromedioYActualizarEstado(userId, cursoId);

            return Ok(new { message = "Nota guardada correctamente ✅" });
        }

        // ==========================================
        // 🔹 Función auxiliar: calcular promedio y actualizar estado
        // ==========================================

        private async Task CalcularPromedioYActualizarEstado(int estudianteId, int cursoId)
        {
            var notas = await _db.Notas
                .Where(n => n.EstudianteId == estudianteId && n.CursoId == cursoId)
                .ToListAsync();

            if (!notas.Any()) return;

            // Pesos oficiales (manteniendo double para evitar errores)
            var pesos = new Dictionary<string, double>
            {
                ["PC1"] = 0.15,
                ["PC2"] = 0.15,
                ["PC3"] = 0.15,
                ["PC4"] = 0.15,
                ["PARCIAL"] = 0.20,
                ["FINAL"] = 0.20
            };

            double suma = 0;
            double totalPesos = 0;

            foreach (var n in notas)
            {
                var nombre = (n.Nombre ?? "").Trim().ToUpperInvariant();
                if (!pesos.TryGetValue(nombre, out var peso)) continue;

                suma += n.NotaValor * peso;
                totalPesos += peso;
            }

            if (totalPesos == 0) return;

            double promedio = suma / totalPesos;
            string estado = promedio >= 11 ? "APROBADO" : "DESAPROBADO";

            var estadoRow = await _db.EstudianteCursoEstados
                .FirstOrDefaultAsync(x => x.EstudianteId == estudianteId && x.CursoId == cursoId);

            if (estadoRow == null)
            {
                estadoRow = new EstudianteCursoEstado
                {
                    EstudianteId = estudianteId,
                    CursoId = cursoId,
                    Estado = estado,
                    NotaFinal = (decimal)promedio,  // ✅ conversión explícita segura
                    UpdatedAt = DateTime.UtcNow
                };
                _db.EstudianteCursoEstados.Add(estadoRow);
            }
            else
            {
                estadoRow.Estado = estado;
                estadoRow.NotaFinal = (decimal)promedio;  // ✅ conversión explícita segura
                estadoRow.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
        }


    }
}
