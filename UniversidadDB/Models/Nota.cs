namespace UniversidadDB.Models
{
    public class Nota
    {
        public int NotaId { get; set; }           // PK
        public int EstudianteId { get; set; }
        public int CursoId { get; set; }

        public string Nombre { get; set; } = string.Empty;
        public double NotaValor { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // Relaciones (si las tienes)
        public Estudiante? Estudiante { get; set; }
        public Curso? Curso { get; set; }
    }
}
