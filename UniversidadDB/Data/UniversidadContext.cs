using Microsoft.EntityFrameworkCore;
using UniversidadDB.Models;

namespace UniversidadDB.Data
{
    public class UniversidadContext : DbContext
    {
        public UniversidadContext(DbContextOptions<UniversidadContext> options) : base(options) { }

        public DbSet<DeviceToken> DeviceTokens { get; set; } = null!;
        public DbSet<Rol> Roles { get; set; } = null!;
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Estudiante> Estudiantes { get; set; } = null!;
        public DbSet<PeriodoAcademico> PeriodosAcademicos { get; set; } = null!;
        public DbSet<Curso> Cursos { get; set; } = null!;
        public DbSet<Inscripcion> Inscripciones { get; set; } = null!;
        public DbSet<NotificacionSistema> NotificacionesSistema { get; set; } = null!;

        // Apuntes & Recordatorios
        public DbSet<Apunte> Apuntes { get; set; } = null!;
        public DbSet<ApunteAdjunto> ApunteAdjuntos { get; set; } = null!;
        public DbSet<AgendaEvento> AgendaEventos { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // === Tablas / Keys (alinear con SQL) ===
            modelBuilder.Entity<Rol>().ToTable("Roles").HasKey(x => x.RolId);

            modelBuilder.Entity<Usuario>().ToTable("Usuarios").HasKey(x => x.UsuarioId);
            modelBuilder.Entity<Estudiante>().ToTable("Estudiantes").HasKey(x => x.EstudianteId);

            modelBuilder.Entity<Curso>().ToTable("Cursos").HasKey(x => x.CursoId);
            modelBuilder.Entity<Inscripcion>().ToTable("Inscripciones").HasKey(x => x.InscripcionId);

            modelBuilder.Entity<PeriodoAcademico>().ToTable("PeriodosAcademicos").HasKey(x => x.Id);

            modelBuilder.Entity<NotificacionSistema>().ToTable("NotificacionesSistema");
            modelBuilder.Entity<DeviceToken>().ToTable("DeviceTokens");

            // === 1 a 1 Usuario -> Estudiante por Estudiante.UsuarioId ===
            modelBuilder.Entity<Estudiante>()
                .HasIndex(e => e.UsuarioId)
                .IsUnique()
                .HasFilter("[UsuarioId] IS NOT NULL"); // SQL Server filtered unique index

            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Estudiante)
                .WithOne(e => e.Usuario)
                .HasForeignKey<Estudiante>(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict); // recomendado (evita cascadas peligrosas)

            // === Rol (si tu Usuario tiene RolId) ===
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Rol)
                .WithMany()
                .HasForeignKey(u => u.RolId)
                .OnDelete(DeleteBehavior.Restrict);

            // === Inscripciones ===
            modelBuilder.Entity<Inscripcion>()
                .HasOne(i => i.Estudiante)
                .WithMany(e => e.Inscripciones)
                .HasForeignKey(i => i.EstudianteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Inscripcion>()
                .HasOne(i => i.Curso)
                .WithMany(c => c.Inscripciones)
                .HasForeignKey(i => i.CursoId)
                .OnDelete(DeleteBehavior.Restrict);

            // === APUNTES ===
            modelBuilder.Entity<Apunte>()
                .ToTable("Apuntes")
                .HasKey(a => a.ApunteId);

            modelBuilder.Entity<ApunteAdjunto>()
                .ToTable("ApunteAdjuntos")
                .HasKey(x => x.AdjuntoId);

            modelBuilder.Entity<Apunte>()
                .HasMany(a => a.Adjuntos)
                .WithOne() // si no tienes navegación ApunteAdjunto.Apunte
                .HasForeignKey(ad => ad.ApunteId)
                .OnDelete(DeleteBehavior.Cascade);

            // === AGENDA / RECORDATORIOS ===
            modelBuilder.Entity<AgendaEvento>()
                .ToTable("AgendaEventos")
                .HasKey(x => x.EventoId);

            base.OnModelCreating(modelBuilder);
        }
    }
}
