public class Docente
{
    public int DocenteId { get; set; }  // Cambié 'Id' a 'DocenteId'
    public string Nombres { get; set; }
    public string Apellidos { get; set; }
    public string Especialidad { get; set; }
    public string FotoUrl { get; set; }
    public bool IsActive { get; set; }  // Usamos 'IsActive' como en la base de datos
}
