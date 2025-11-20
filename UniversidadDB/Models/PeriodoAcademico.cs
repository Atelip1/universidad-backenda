using System.ComponentModel.DataAnnotations;

public class PeriodoAcademico
{
    [Key] // Esta es la anotación para definir la clave primaria
    public int Id { get; set; } // Suponiendo que "Id" es la clave primaria

    public string Nombre { get; set; }
    public int Anio { get; set; }
    public string Ciclo { get; set; }
    // Otras propiedades...
}
