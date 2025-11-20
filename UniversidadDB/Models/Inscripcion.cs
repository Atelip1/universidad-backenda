namespace UniversidadDB.Models
{
    public class Inscripcion
    {
        public int InscripcionId { get; set; }

        public int EstudianteId { get; set; }
        public Estudiante Estudiante { get; set; } = null!;

        public int CursoId { get; set; }
        public Curso Curso { get; set; } = null!;

        public DateTime FechaInscripcion { get; set; } = DateTime.Now;

        public string Estado { get; set; } = "ACTIVO";
    }
}
