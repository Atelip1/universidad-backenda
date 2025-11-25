using Microsoft.EntityFrameworkCore;
using UniversidadDB.Models;

namespace UniversidadDB.Data
{
    public class UniversidadContext : DbContext
    {
        public UniversidadContext(DbContextOptions<UniversidadContext> options)
            : base(options)
        {
        }
        public DbSet<DeviceToken> DeviceTokens { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Estudiante> Estudiantes { get; set; }
        public DbSet<PeriodoAcademico> PeriodosAcademicos { get; set; }
        public DbSet<Curso> Cursos { get; set; }
        public DbSet<Inscripcion> Inscripciones { get; set; }
        public DbSet<NotificacionSistema> NotificacionesSistema { get; set; }
        public DbSet<NotificacionSistema> NotificacionesSistemaHistorial { get; set; } // (si fuera otro)


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Relación 1 a 1 entre Usuario y Estudiante
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Estudiante)
                .WithOne(e => e.Usuario)
                .HasForeignKey<Estudiante>(e => e.EstudianteId);

            // Relación 1 a muchos entre Estudiante e Inscripciones
            modelBuilder.Entity<Inscripcion>()
                .HasOne(i => i.Estudiante)
                .WithMany(e => e.Inscripciones)
                .HasForeignKey(i => i.EstudianteId);

            // Relación 1 a muchos entre Curso e Inscripciones
            modelBuilder.Entity<Inscripcion>()
                .HasOne(i => i.Curso)
                .WithMany(c => c.Inscripciones)
                .HasForeignKey(i => i.CursoId);

            // Puedes agregar otras configuraciones de relación según sea necesario
            // Ejemplo de relación 1 a muchos con PeriodoAcademico (si es necesario)
            modelBuilder.Entity<PeriodoAcademico>()
                .HasKey(p => p.Id); // Si la clave primaria no está definida

            base.OnModelCreating(modelBuilder);
        }
    }
}
