
// Models/Backup.cs
public class Backup
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Size { get; set; }
    public string Status { get; set; } // "OK" o "ERROR"
}
