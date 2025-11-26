
using UniversidadDB.Models.DTOs;

namespace UniversidadDB.Models.DTOs
{
    public class NuevoCursoMallaDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public int Ciclo { get; set; }
        public int Creditos { get; set; }
        public bool Obligatorio { get; set; }
        public bool Activo { get; set; }
    }
}
