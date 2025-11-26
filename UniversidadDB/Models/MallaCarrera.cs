using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniversidadDB.Models
{
    [Table("MallaCarrera")]
    public class MallaCarrera
    {
        [Key]
        public int MallaId { get; set; }  // o MallaCarreraId, si prefieres

        public int CarreraId { get; set; }
        public int CursoId { get; set; }
        public int Ciclo { get; set; }
        public int Creditos { get; set; }
        public bool Obligatorio { get; set; }
        public bool Activo { get; set; }

        // Relaciones
        public Carrera? Carrera { get; set; }
        public Curso? Curso { get; set; }
    }
}
