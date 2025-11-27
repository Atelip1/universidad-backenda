public class Notificacion
{
    public int NotificacionId { get; set; }
    public int UsuarioId { get; set; }  // Relacionado con el Usuario
    public string Titulo { get; set; }
    public string Mensaje { get; set; }
    public DateTime FechaCreacion { get; set; }
    public bool Leida { get; set; }
    public string Tipo { get; set; }   // "Alerta", "Recordatorio", "Información"
    public string Canal { get; set; }  // "App", "Email", "SMS"
}
