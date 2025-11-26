using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Mail;
using UniversidadDB.Models;

namespace UniversidadDB.Data
{
    public class UniversidadContext : DbContext
    {
        public DbSet<MallaCarrera> MallaCarrera { get; set; }

        public UniversidadContext(DbContextOptions<UniversidadContext> options) : base(options) { }
        public DbSet<Carrera> Carreras { get; set; } = null!;
        public DbSet<Prerequisito> Prerequisitos { get; set; } = null!;
        public DbSet<EstudianteCursoEstado> EstudianteCursoEstados { get; set; } = null!;
        public DbSet<CursoMaterial> CursoMateriales { get; set; } = null!;

        public DbSet<DeviceToken> DeviceTokens { get; set; } = null!;
        public DbSet<Rol> Roles { get; set; } = null!;
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Estudiante> Estudiantes { get; set; } = null!;
        public DbSet<PeriodoAcademico> PeriodosAcademicos { get; set; } = null!;
        public DbSet<Curso> Cursos { get; set; } = null!;
        public DbSet<Inscripcion> Inscripciones { get; set; } = null!;
        public DbSet<NotificacionSistema> NotificacionesSistema { get; set; } = null!;
        public DbSet<Nota> Notas { get; set; }
        public DbSet<MaterialCurso> MaterialCursos { get; set; }

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

           
            // === 1 a 1 Usuario -> Estudiante (Shared Key: Estudiante.EstudianteId = Usuario.UsuarioId) ===
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Estudiante)
                .WithOne(e => e.Usuario)
                .HasForeignKey<Estudiante>(e => e.EstudianteId)
                .OnDelete(DeleteBehavior.Restrict);



            // === Rol (Usuario -> Rol por RolId) ===
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Rol)
                .WithMany(r => r.Usuarios)
                .HasForeignKey(u => u.RolId)
                .HasConstraintName("FK_Usuarios_Roles")
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
            modelBuilder.Entity<Carrera>().ToTable("Carreras").HasKey(x => x.CarreraId);

            modelBuilder.Entity<MallaCarrera>().ToTable("MallaCarrera")
                 .HasKey(x => x.MallaId);


            modelBuilder.Entity<Prerequisito>().ToTable("Prerequisitos")
                .HasKey(x => new { x.CursoId, x.CursoPrereqId });

            modelBuilder.Entity<EstudianteCursoEstado>().ToTable("EstudianteCursoEstado")
                .HasKey(x => new { x.EstudianteId, x.CursoId });

            modelBuilder.Entity<CursoMaterial>().ToTable("CursoMateriales")
                .HasKey(x => x.MaterialId);


            modelBuilder.Entity<EstudianteCursoEstado>(entity =>
            {
                entity.Property(x => x.NotaFinal)
                      .HasPrecision(4, 2); // 0.00 a 99.99, suficiente para notas 0-20
            });


            base.OnModelCreating(modelBuilder);
        }
    }
}
