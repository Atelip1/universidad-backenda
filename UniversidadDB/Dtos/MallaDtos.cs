namespace UniversidadDB.Dtos;

public record MallaUpsertDto(
    int CursoId,
    int Ciclo,
    int? Creditos,
    bool Obligatorio,
    bool Activo
);

public record EstadoCursoUpdateDto(
    string Estado,     // PENDIENTE | EN_CURSO | APROBADO | REPROBADO
    int? PeriodoId,
    decimal? NotaFinal
);
