public class NotificacionDto
{
    public string Titulo { get; set; }
    public string Mensaje { get; set; }
    public string Tipo { get; set; }  // "Alerta", "Recordatorio", "Información"
    public string Canal { get; set; } // "App", "Email", "SMS"
}
