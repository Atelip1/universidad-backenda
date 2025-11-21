using Microsoft.EntityFrameworkCore;
using UniversidadDB.Data;
using System;

namespace UniversidadDB.Services
{
    public class DatabaseService
    {
        private readonly UniversidadContext _context;

        // Constructor: Obtiene la cadena de conexión desde la variable de entorno
        public DatabaseService()
        {
            // Obtener la cadena de conexión desde la variable de entorno
            string connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("La cadena de conexión no está configurada.");
            }

            // Configurar DbContext con la cadena de conexión
            var optionsBuilder = new DbContextOptionsBuilder<UniversidadContext>();
            optionsBuilder.UseSqlServer(connectionString);

            _context = new UniversidadContext(optionsBuilder.Options);
        }

        // Método para obtener el contexto y acceder a la base de datos
        public UniversidadContext GetContext()
        {
            return _context;
        }
    }
}
