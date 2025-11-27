// Controllers/BackupController.cs
using Microsoft.AspNetCore.Mvc;
using UniversidadDB.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UniversidadDB.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BackupController : ControllerBase
    {
        private static List<Backup> backups = new List<Backup>();

        // Obtener lista de copias de seguridad
        [HttpGet]
        public IActionResult GetBackups()
        {
            if (backups.Count == 0)
                return NotFound("No hay copias de seguridad disponibles.");

            return Ok(backups);
        }

        // Crear una nueva copia de seguridad
        [HttpPost]
        public IActionResult CreateBackup()
        {
            var newBackup = new Backup
            {
                Id = backups.Count + 1,
                CreatedAt = DateTime.Now,
                Size = "100 MB", // Tamaño simulado
                Status = "OK"    // Estado simulado
            };

            backups.Add(newBackup);
            return CreatedAtAction(nameof(GetBackups), new { id = newBackup.Id }, newBackup);
        }
    }
}
