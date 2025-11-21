using System.Security.Cryptography;

public string GenerateResetToken()
{
    // Genera un token aleatorio
    using (var rng = new RNGCryptoServiceProvider())
    {
        var buffer = new byte[32]; // Tamaño de 256 bits
        rng.GetBytes(buffer);
        return Convert.ToBase64String(buffer); // Convierte a Base64 para usarlo como token
    }
}
