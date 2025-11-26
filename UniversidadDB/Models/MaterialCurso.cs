using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniversidadDB.Models
{
    public class MaterialCurso
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaterialId { get; set; }

        [Required]
        public int CursoId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        public string Ruta { get; set; } = string.Empty; // Ruta física o URL del archivo

        public DateTime FechaSubida { get; set; } = DateTime.UtcNow;

        public int SubidoPor { get; set; } // Id del usuario/estudiante

        public bool VisibleParaTodos { get; set; } = true;

        // 🔗 Relaciones
        [ForeignKey(nameof(CursoId))]
        public Curso? Curso { get; set; }
    }
}
