namespace UniversidadDB.Models.Auth
{
    public class RegisterEstudianteRequest
    {
        public string NombreCompleto { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;

        // Datos extra del estudiante
        public string? CodigoEstudiante { get; set; }
        public string? Carrera { get; set; }
        public int? Ciclo { get; set; }
    }
}