
using UniversidadDB.Models.Comunidad;

public class Post
{
    public int Id { get; set; }
    public int AutorId { get; set; } // FK a Usuario o Estudiante
    public string Contenido { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public string? EtiquetaCurso { get; set; } // ej. "#Matemática"
    public bool Oculto { get; set; } = false;

    // Relaciones
    public List<Comentario> Comentarios { get; set; } = new();
    public List<Like> Likes { get; set; } = new();
}
