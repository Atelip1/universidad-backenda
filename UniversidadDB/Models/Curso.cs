namespace UniversidadDB.Models
{
    public class Curso
    {
        public int CursoId { get; set; }
        public int? PeriodoId { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public int? Creditos { get; set; }
        public string? ColorHex { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        // 🆕 Agrega este campo
        public string? Codigo { get; set; }
        public ICollection<MallaCarrera>? Mallas { get; set; }

        public PeriodoAcademico? Periodo { get; set; }

        public ICollection<Inscripcion> Inscripciones { get; set; } = new List<Inscripcion>();
    }
}
