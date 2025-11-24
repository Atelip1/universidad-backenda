using System;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;

public class EmailService
{
    // La API KEY viene del Environment en Render (SENDGRID_API_KEY)
    private readonly string _sendGridApiKey =
        Environment.GetEnvironmentVariable("SENDGRID_API_KEY") ?? "";

    /// <summary>
    /// Envía el correo con el código de recuperación de contraseña.
    /// </summary>
    public async Task SendPasswordResetEmail(string toEmail, string userName, string code)
    {
        if (string.IsNullOrWhiteSpace(_sendGridApiKey))
        {
            throw new InvalidOperationException("No se ha configurado la SendGrid API Key.");
        }

        var client = new SendGridClient(_sendGridApiKey);

        // ⬇⬇⬇ PON AQUÍ EL CORREO QUE TIENES VERIFICADO EN SENDGRID ⬇⬇⬇
        var from = new EmailAddress("mariadelpilartasaycolaque@gmail.com", "Portal Académico UPSJB");
        // ⬆⬆⬆ SOLO CAMBIA ESA DIRECCIÓN, NADA MÁS ⬆⬆⬆

        var to = new EmailAddress(toEmail, userName);
        var subject = "Código para restablecer tu contraseña";

        var plainTextContent =
            $"Hola {userName},\n\n" +
            $"Tu código para restablecer la contraseña es: {code}\n\n" +
            "Si tú no solicitaste este correo, puedes ignorarlo.";

        var htmlContent =
            $"<p>Hola <strong>{userName}</strong>,</p>" +
            $"<p>Tu código para restablecer la contraseña es:</p>" +
            $"<h2>{code}</h2>" +
            $"<p>Si tú no solicitaste este correo, puedes ignorar este mensaje.</p>";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

        var response = await client.SendEmailAsync(msg);

        if ((int)response.StatusCode >= 400)
        {
            var body = await response.Body.ReadAsStringAsync();
            throw new Exception($"Error al enviar el correo: {response.StatusCode} - {body}");
        }
    }
}
