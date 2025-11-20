namespace UniversidadDB.Models
{
    public class Estudiante
    {
        public int EstudianteId { get; set; }   // FK = UsuarioId
        public string? CodigoEstudiante { get; set; }
        public string? Carrera { get; set; }
        public int? Ciclo { get; set; }
        public DateTime? FechaIngreso { get; set; }

        // Relación 1:1 con Usuario
        public Usuario Usuario { get; set; } = null!;

        // Colección de inscripciones
        public ICollection<Inscripcion> Inscripciones { get; set; } = new List<Inscripcion>();
    }
}