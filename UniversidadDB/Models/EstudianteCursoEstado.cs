namespace UniversidadDB.Models;

public class EstudianteCursoEstado
{
    public int EstudianteId { get; set; }
    public int CursoId { get; set; }

    public string Estado { get; set; } = "PENDIENTE"; // PENDIENTE|EN_CURSO|APROBADO|REPROBADO
    public int? PeriodoId { get; set; }
    public decimal? NotaFinal { get; set; }

    public DateTime UpdatedAt { get; set; }
}