using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversidadDB.Data;
using UniversidadDB.Dtos;
using UniversidadDB.Helpers;

namespace UniversidadDB.Controllers;

[ApiController]
[Route("api/admin/materiales")]
[Authorize(Roles = "ADMIN")]
public class AdminMaterialesController : ControllerBase
{
    private readonly UniversidadContext _db;
    public AdminMaterialesController(UniversidadContext db) => _db = db;

    // GET /api/admin/materiales/pending?cursoId=1
    [HttpGet("pending")]
    public async Task<IActionResult> Pending([FromQuery] int? cursoId)
    {
        var q = _db.CursoMateriales.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Source == "STUDENT" && x.Status == "PENDING");

        if (cursoId.HasValue) q = q.Where(x => x.CursoId == cursoId.Value);

        var data = await q.OrderByDescending(x => x.CreatedAt).ToListAsync();
        return Ok(data);
    }

    // PUT /api/admin/materiales/{id}/aprobar
    [HttpPut("{id:int}/aprobar")]
    public async Task<IActionResult> Aprobar(int id)
    {
        var mat = await _db.CursoMateriales.FirstOrDefaultAsync(x => x.MaterialId == id && !x.IsDeleted);
        if (mat == null) return NotFound();

        var adminId = AuthHelpers.GetUserId(User);
        var now = DateTime.UtcNow;

        mat.Status = "APPROVED";
        mat.ApprovedByUsuarioId = adminId;
        mat.ApprovedAt = now;
        mat.RejectedReason = null;
        mat.UpdatedAt = now;

        await _db.SaveChangesAsync();
        return Ok(mat);
    }

    // PUT /api/admin/materiales/{id}/rechazar
    [HttpPut("{id:int}/rechazar")]
    public async Task<IActionResult> Rechazar(int id, [FromBody] MaterialRejectDto dto)
    {
        var mat = await _db.CursoMateriales.FirstOrDefaultAsync(x => x.MaterialId == id && !x.IsDeleted);
        if (mat == null) return NotFound();

        var now = DateTime.UtcNow;

        mat.Status = "REJECTED";
        mat.RejectedReason = string.IsNullOrWhiteSpace(dto.Reason) ? "Sin detalle" : dto.Reason.Trim();
        mat.UpdatedAt = now;

        await _db.SaveChangesAsync();
        return Ok(mat);
    }

    // DELETE /api/admin/materiales/{id}  (soft delete)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var mat = await _db.CursoMateriales.FirstOrDefaultAsync(x => x.MaterialId == id && !x.IsDeleted);
        if (mat == null) return NotFound();

        mat.IsDeleted = true;
        mat.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
