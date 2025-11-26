namespace UniversidadDB.Dtos;

public record ApunteCreateDto(int? CursoId, string Titulo, string Contenido, bool IsPinned);
public record ApunteUpdateDto(int? CursoId, string Titulo, string Contenido, bool IsPinned);

public record ApunteAdjuntoCreateDto(string Tipo, string Url, string? FileName, string? MimeType);
