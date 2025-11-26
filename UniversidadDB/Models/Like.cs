using System;

namespace UniversidadDB.Models.Comunidad
{
    public class Like
    {
        // Usaremos clave compuesta (PostId + UsuarioId) en el DbContext
        public int PostId { get; set; }
        public int UsuarioId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public Post? Post { get; set; }
    }
}
