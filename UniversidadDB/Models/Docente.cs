public class Docente
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public string Especialidad { get; set; }
    public string FotoUrl { get; set; } // Para guardar la URL de la foto
    public bool Activo { get; set; }  // Campo para activar/desactivar docente
}
