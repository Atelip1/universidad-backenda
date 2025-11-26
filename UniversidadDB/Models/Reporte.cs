public class Reporte
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int? PostId { get; set; }
    public int? ComentarioId { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
