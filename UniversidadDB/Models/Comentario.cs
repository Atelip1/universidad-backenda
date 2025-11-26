public class Comentario
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public int AutorId { get; set; }
    public string Contenido { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public bool Oculto { get; set; } = false;
}
