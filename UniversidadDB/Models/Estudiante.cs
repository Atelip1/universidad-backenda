using System;
using System.Collections.Generic;

namespace UniversidadDB.Models
{
    public class Estudiante
    {
        // ✅ Shared Key: EstudianteId == UsuarioId
        public int EstudianteId { get; set; }

        public string? CodigoEstudiante { get; set; }

        // ✅ Lo usas en LoginResponse: user.Estudiante?.Carrera
        public string? Carrera { get; set; }

        // ✅ Para la malla por carrera
        public int? CarreraId { get; set; }

        public int? Ciclo { get; set; }
        public DateTime FechaIngreso { get; set; } = DateTime.UtcNow;

        // Navegaciones
        public Usuario? Usuario { get; set; }
        public ICollection<Inscripcion> Inscripciones { get; set; } = new List<Inscripcion>();
    }
}
