using System.ComponentModel.DataAnnotations;

namespace UniversidadDB.Models.Auth
{
    public class ForgotPasswordRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;
    }
}
