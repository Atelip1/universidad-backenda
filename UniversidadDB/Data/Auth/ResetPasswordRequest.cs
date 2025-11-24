using System.ComponentModel.DataAnnotations;

namespace UniversidadDB.Models.Auth
{
    public class ResetPasswordRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string ResetCode { get; set; } = null!;

        [Required]
        public string NewPassword { get; set; } = null!;
    }
}
