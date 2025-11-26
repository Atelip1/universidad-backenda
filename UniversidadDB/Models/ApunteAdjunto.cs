namespace UniversidadDB.Models;

public class ApunteAdjunto
{
    public int AdjuntoId { get; set; }
    public int ApunteId { get; set; }

    public string Tipo { get; set; } = "LINK"; // LINK / PDF / IMG
    public string Url { get; set; } = "";
    public string? FileName { get; set; }
    public string? MimeType { get; set; }

    public DateTime CreatedAt { get; set; }
}
