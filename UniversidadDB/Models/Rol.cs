namespace UniversidadDB.Models
{
    public class Rol
    {
        public int RolId { get; set; }
        public string NombreRol { get; set; } = null!;

        // Navegación
        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}
