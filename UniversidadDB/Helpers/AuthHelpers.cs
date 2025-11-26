using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using UniversidadDB.Data;

namespace UniversidadDB.Helpers;

public static class AuthHelpers
{
    public static int GetUserId(ClaimsPrincipal user)
    {
        // Soporta varios claims comunes según cómo generes el JWT
        var idStr =
            user.FindFirstValue(ClaimTypes.NameIdentifier) ??
            user.FindFirstValue("userId") ??
            user.FindFirstValue("UsuarioId") ??
            user.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(idStr))
            throw new UnauthorizedAccessException("Token sin userId válido.");

        // A veces 'sub' puede venir como GUID/string. Aquí forzamos int.
        if (!int.TryParse(idStr, out var userId) || userId <= 0)
            throw new UnauthorizedAccessException("Token con userId inválido (no es int).");

        return userId;
    }

    /// <summary>
    /// En tu BD actual: Estudiantes.EstudianteId == Usuarios.UsuarioId (PK compartida).
    /// </summary>
    public static async Task<int> GetEstudianteIdAsync(UniversidadContext db, int userId)
    {
        var existe = await db.Estudiantes.AsNoTracking()
            .AnyAsync(e => e.EstudianteId == userId);

        if (!existe)
            throw new UnauthorizedAccessException("Este usuario no está vinculado a un estudiante.");

        return userId; // ✅ PK compartida
    }
}
