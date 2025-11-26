namespace UniversidadDB.Models
{
    public class Estudiante
    {
        public int EstudianteId { get; set; }    // ✅ PK normal (NO es FK)
        public int? UsuarioId { get; set; }      // ✅ FK a Usuarios.UsuarioId

        public string? CodigoEstudiante { get; set; }
        public string? Carrera { get; set; }
        public int? Ciclo { get; set; }
        public DateTime? FechaIngreso { get; set; }

        // Relación 1:1 con Usuario (opcional si UsuarioId es null)
        public Usuario? Usuario { get; set; }

        public ICollection<Inscripcion> Inscripciones { get; set; } = new List<Inscripcion>();
    }
}
