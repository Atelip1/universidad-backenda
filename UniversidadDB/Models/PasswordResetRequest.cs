using System.ComponentModel.DataAnnotations;

namespace UniversidadDB.Models.Auth
{
    public class PasswordResetRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;
    }
}
