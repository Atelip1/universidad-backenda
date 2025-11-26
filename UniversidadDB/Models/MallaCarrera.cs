namespace UniversidadDB.Models;

public class MallaCarrera
{
    public int CarreraId { get; set; }
    public int CursoId { get; set; }

    public int Ciclo { get; set; }
    public int? Creditos { get; set; }
    public bool Obligatorio { get; set; } = true;
    public bool Activo { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
