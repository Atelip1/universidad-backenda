using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using UniversidadDB.Data;

namespace UniversidadDB.Helpers;

public static class AuthHelpers
{
    public static int GetUserId(ClaimsPrincipal user)
    {
        var idStr =
            user.FindFirstValue(ClaimTypes.NameIdentifier) ??
            user.FindFirstValue("sub") ??
            user.FindFirstValue("userId");

        if (string.IsNullOrWhiteSpace(idStr) || !int.TryParse(idStr, out var userId))
            throw new UnauthorizedAccessException("Token sin userId válido.");

        return userId;
    }

    public static async Task<int> GetEstudianteIdAsync(UniversidadContext db, int userId)
    {
        var estudianteId = await db.Estudiantes.AsNoTracking()
            .Where(e => e.UsuarioId == userId)
            .Select(e => e.EstudianteId)
            .FirstOrDefaultAsync();

        if (estudianteId == 0)
            throw new UnauthorizedAccessException("Este usuario no está vinculado a un estudiante.");

        return estudianteId;
    }
}
