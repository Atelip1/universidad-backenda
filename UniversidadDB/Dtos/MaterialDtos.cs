namespace UniversidadDB.Dtos;

public record MaterialCreateDto(
    string Titulo,
    string Url,
    string? Descripcion,
    string? FileName,
    string? MimeType
);

public record MaterialRejectDto(string? Reason);
