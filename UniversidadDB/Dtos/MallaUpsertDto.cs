namespace UniversidadDB.Dtos;

public class MallaUpsertDto
{
    public int CursoId { get; set; }
    public int Ciclo { get; set; }
    public int Creditos { get; set; }
    public bool Obligatorio { get; set; }
    public bool Activo { get; set; } = true;
}
