using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversidadDB.Data;
using UniversidadDB.Dtos;
using UniversidadDB.Models;

namespace UniversidadDB.Controllers
{
    [ApiController]
    [Route("api/admin/malla")]
    [Authorize(Roles = "ADMIN")]
    public class AdminMallaController : ControllerBase
    {
        private readonly UniversidadContext _db;
        public AdminMallaController(UniversidadContext db) => _db = db;

        // ✅ GET /api/admin/malla/carreras
        // Devuelve todas las carreras (para llenar el combo en Flutter)
        [HttpGet("carreras")]
        public async Task<IActionResult> GetCarreras()
        {
            var carreras = await _db.Carreras
                .Select(c => new { c.CarreraId, c.Nombre })
                .ToListAsync();

            return Ok(carreras);
        }

        // ✅ GET /api/admin/malla/carreras/{carreraId}
        // Devuelve la malla completa de una carrera
        [HttpGet("carreras/{carreraId:int}")]
        public async Task<IActionResult> GetMallaCarrera(int carreraId)
        {
            var carreraExists = await _db.Carreras.AsNoTracking()
                .AnyAsync(x => x.CarreraId == carreraId);

            if (!carreraExists)
                return NotFound("Carrera no existe.");

            var data = await (from m in _db.MallaCarrera.AsNoTracking()
                              join c in _db.Cursos.AsNoTracking() on m.CursoId equals c.CursoId
                              where m.CarreraId == carreraId
                              orderby m.Ciclo, c.Nombre
                              select new
                              {
                                  m.CarreraId,
                                  m.CursoId,
                                  CursoNombre = c.Nombre,
                                  m.Ciclo,
                                  m.Creditos,
                                  m.Obligatorio,
                                  m.Activo
                              }).ToListAsync();

            return Ok(data);
        }

        // ✅ POST /api/admin/malla/carreras/{carreraId}
        // Crea o actualiza un ítem de la malla
        [HttpPost("carreras/{carreraId:int}")]
        public async Task<IActionResult> UpsertMallaItem(int carreraId, [FromBody] MallaUpsertDto dto)
        {
            if (dto == null)
                return BadRequest("Body requerido.");

            var carreraExists = await _db.Carreras.AnyAsync(x => x.CarreraId == carreraId);
            if (!carreraExists) return NotFound("Carrera no existe.");

            var cursoExists = await _db.Cursos.AnyAsync(x => x.CursoId == dto.CursoId);
            if (!cursoExists) return NotFound("Curso no existe.");

            if (dto.Ciclo <= 0)
                return BadRequest("Ciclo inválido.");
            if (dto.Creditos < 0)
                return BadRequest("Créditos inválidos.");

            var row = await _db.MallaCarrera
                .FirstOrDefaultAsync(x => x.CarreraId == carreraId && x.CursoId == dto.CursoId);

            if (row == null)
            {
                row = new MallaCarrera
                {
                    CarreraId = carreraId,
                    CursoId = dto.CursoId
                };
                _db.MallaCarrera.Add(row);
            }

            row.Ciclo = dto.Ciclo;
            row.Creditos = dto.Creditos;
            row.Obligatorio = dto.Obligatorio;
            row.Activo = dto.Activo;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                row.CarreraId,
                row.CursoId,
                row.Ciclo,
                row.Creditos,
                row.Obligatorio,
                row.Activo
            });
        }

        // ✅ DELETE /api/admin/malla/carreras/{carreraId}/{cursoId}
        [HttpDelete("carreras/{carreraId:int}/{cursoId:int}")]
        public async Task<IActionResult> DeleteMallaItem(int carreraId, int cursoId)
        {
            var row = await _db.MallaCarrera
                .FirstOrDefaultAsync(x => x.CarreraId == carreraId && x.CursoId == cursoId);

            if (row == null) return NotFound();

            _db.MallaCarrera.Remove(row);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // ✅ POST /api/admin/malla/prerequisitos/{cursoId}/{cursoPrereqId}
        [HttpPost("prerequisitos/{cursoId:int}/{cursoPrereqId:int}")]
        public async Task<IActionResult> AddPrereq(int cursoId, int cursoPrereqId)
        {
            if (cursoId == cursoPrereqId)
                return BadRequest("No puede ser prerequisito de sí mismo.");

            var exists1 = await _db.Cursos.AnyAsync(x => x.CursoId == cursoId);
            var exists2 = await _db.Cursos.AnyAsync(x => x.CursoId == cursoPrereqId);
            if (!exists1 || !exists2)
                return NotFound("Curso o prerequisito no existe.");

            var already = await _db.Prerequisitos
                .AnyAsync(x => x.CursoId == cursoId && x.CursoPrereqId == cursoPrereqId);

            if (already)
                return Ok(new { CursoId = cursoId, CursoPrereqId = cursoPrereqId, Exists = true });

            _db.Prerequisitos.Add(new Prerequisito
            {
                CursoId = cursoId,
                CursoPrereqId = cursoPrereqId
            });

            await _db.SaveChangesAsync();
            return Ok(new { CursoId = cursoId, CursoPrereqId = cursoPrereqId, Exists = false });
        }

        // ✅ DELETE /api/admin/malla/prerequisitos/{cursoId}/{cursoPrereqId}
        [HttpDelete("prerequisitos/{cursoId:int}/{cursoPrereqId:int}")]
        public async Task<IActionResult> RemovePrereq(int cursoId, int cursoPrereqId)
        {
            var row = await _db.Prerequisitos
                .FirstOrDefaultAsync(x => x.CursoId == cursoId && x.CursoPrereqId == cursoPrereqId);

            if (row == null)
                return NotFound();

            _db.Prerequisitos.Remove(row);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
