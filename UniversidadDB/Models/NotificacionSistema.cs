using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniversidadDB.Models
{
    [Table("NotificacionesSistema", Schema = "dbo")] // 👈 CAMBIA si tu tabla se llama distinto
    public class NotificacionSistema
    {
        [Key]
        public int NotificacionId { get; set; }

        public int UsuarioId { get; set; }

        [MaxLength(150)]
        public string Titulo { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Mensaje { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public bool Leida { get; set; } = false;
    }
}
