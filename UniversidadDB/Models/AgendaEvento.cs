using System.ComponentModel.DataAnnotations;

namespace UniversidadDB.Models;

public class AgendaEvento
{
    public int EventoId { get; set; }
    public int EstudianteId { get; set; }
    public int? CursoId { get; set; }

    [MaxLength(200)]
    public string Titulo { get; set; } = "";

    [MaxLength(500)]
    public string? Nota { get; set; }

    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }

    [MaxLength(80)]
    public string? RepeatRule { get; set; }

    public int? ReminderMinutesBefore { get; set; }

    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
