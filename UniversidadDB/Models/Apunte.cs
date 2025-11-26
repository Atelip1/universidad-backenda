namespace UniversidadDB.Models;

public class Apunte
{
    public int ApunteId { get; set; }
    public int EstudianteId { get; set; }
    public int? CursoId { get; set; }

    public string Titulo { get; set; } = "";
    public string Contenido { get; set; } = "";
    public bool IsPinned { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // ✅ Esto es lo que te faltaba para que exista "Adjuntos"
    public List<ApunteAdjunto> Adjuntos { get; set; } = new();
}
