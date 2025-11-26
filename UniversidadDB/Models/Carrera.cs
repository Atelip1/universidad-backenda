namespace UniversidadDB.Models;

public class Carrera
{
    public int CarreraId { get; set; }
    public string Nombre { get; set; } = "";
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
