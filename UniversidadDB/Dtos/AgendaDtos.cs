namespace UniversidadDB.Dtos;

public record AgendaCreateDto(
    int? CursoId,
    string Titulo,
    string? Nota,
    DateTime StartAt,
    DateTime EndAt,
    string? RepeatRule,
    int? ReminderMinutesBefore
);

public record AgendaUpdateDto(
    int? CursoId,
    string Titulo,
    string? Nota,
    DateTime StartAt,
    DateTime EndAt,
    string? RepeatRule,
    int? ReminderMinutesBefore,
    bool IsCompleted
);
