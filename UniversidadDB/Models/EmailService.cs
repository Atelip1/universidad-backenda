using System;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace UniversidadDB.Models
{
    public class EmailService
    {
        /// <summary>
        /// Envía el correo con el código de recuperación de contraseña usando SendGrid.
        /// La API Key se lee desde la variable de entorno SENDGRID_API_KEY (configurada en Render).
        /// </summary>
        public async Task SendPasswordResetEmail(string toEmail, string userName, string code)
        {
            // 👇 IMPORTANTE: la API key ya NO está en el código.
            var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("No se ha configurado la SendGrid API Key.");
            }

            var client = new SendGridClient(apiKey);

            var from = new EmailAddress("no-reply@upsjb.edu.pe", "Portal Académico UPSJB");
            var to = new EmailAddress(toEmail, userName);
            var subject = "Código para restablecer tu contraseña";

            var plainTextContent =
                $"Hola {userName},\n\n" +
                $"Tu código para restablecer la contraseña es: {code}\n\n" +
                "Si tú no solicitaste este correo, puedes ignorarlo.";

            var htmlContent =
                $"<p>Hola <strong>{userName}</strong>,</p>" +
                "<p>Tu código para restablecer la contraseña es:</p>" +
                $"<h2>{code}</h2>" +
                "<p>Si tú no solicitaste este correo, puedes ignorar este mensaje.</p>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);

            if ((int)response.StatusCode >= 400)
            {
                var body = await response.Body.ReadAsStringAsync();
                throw new Exception($"Error al enviar el correo: {response.StatusCode} - {body}");
            }
        }
    }
}
