namespace UniversidadDB.Models.Auth
{
    public class LoginRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? ResetCode { get; set; }           // código de 6 dígitos
        public DateTime? ResetCodeExpiration { get; set; }  // fecha de expiración
    }
}
}