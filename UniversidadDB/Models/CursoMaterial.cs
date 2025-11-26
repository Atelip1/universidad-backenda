namespace UniversidadDB.Models;

public class CursoMaterial
{
    public int MaterialId { get; set; }
    public int CursoId { get; set; }

    public int? AutorUsuarioId { get; set; }
    public int? ApprovedByUsuarioId { get; set; }

    public string Source { get; set; } = "ADMIN";   // ADMIN | STUDENT
    public string Status { get; set; } = "APPROVED"; // PENDING | APPROVED | REJECTED

    public string Titulo { get; set; } = "";
    public string? Descripcion { get; set; }
    public string Url { get; set; } = "";

    public string? FileName { get; set; }
    public string? MimeType { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectedReason { get; set; }

    public bool IsDeleted { get; set; }
}
