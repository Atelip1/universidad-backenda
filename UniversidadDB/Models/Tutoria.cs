public class Tutoria
{
    public int Id { get; set; }
    public string Titulo { get; set; }
    public string Docente { get; set; }
    public DateTime FechaInicio { get; set; }
    public TimeSpan Duracion { get; set; }
    public string Modalidad { get; set; } // Virtual / Presencial / Híbrida
    public string Estado { get; set; } // Programada, Completada, Pendiente, Cancelada
    public string Aula { get; set; }
    public int Inscritos { get; set; }
    public int Capacidad { get; set; }
    public string ImageUrl { get; set; }
}
