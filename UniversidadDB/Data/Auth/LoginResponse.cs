namespace UniversidadDB.Models.Auth
{
    public class LoginResponse
    {
        public int UsuarioId { get; set; }
        public string NombreCompleto { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Rol { get; set; } = null!;   // "ADMIN" o "ESTUDIANTE"
        public int? EstudianteId { get; set; }     // null si es ADMIN

        public string? Carrera { get; set; }       // 👈 NUEVO
        public string? CodigoEstudiante { get; set; }  // 👈 NUEVO
        public int? Ciclo { get; set; }                // 👈 NUEVO

        public string Message { get; set; } = null!;
    }
}
