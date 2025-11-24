using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace UniversidadDB.Services
{
    public class EmailService
    {
        private readonly string _smtpServer = "smtp.gmail.com"; // Servidor SMTP de Gmail
        private readonly int _smtpPort = 587;                    // Puerto SMTP para TLS
        private readonly string _smtpUsername = "mariadelpilartasaycolaque@gmail.com";
        private readonly string _smtpPassword = "snmj mwjx cxja zcaj"; // Contraseña de aplicación

        // 📧 1) Correo de bienvenida
        public async Task SendWelcomeEmail(string toEmail, string userName)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Universidad", "no-reply@upsjb.edu.pe"));
            message.To.Add(new MailboxAddress(userName, toEmail));
            message.Subject = "Bienvenido a la Universidad";

            var bodyBuilder = new BodyBuilder
            {
                TextBody = $"Hola {userName},\n\nBienvenido a la plataforma de la universidad.\n\nSaludos,\nEquipo Académico"
            };

            message.Body = bodyBuilder.ToMessageBody();

            await SendAsync(message);
        }

        // 📧 2) Correo para restablecer contraseña
        //    Ajusta los parámetros si en tu AuthController usas otros (pero casi seguro son email + token)
        public async Task SendPasswordResetEmail(string toEmail, string resetToken)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Universidad", "no-reply@upsjb.edu.pe"));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Restablecer contraseña";

            // 🔗 Puedes cambiar esta URL por la de tu frontend real
            var resetLink =
                $"https://mi-app-universidad.com/reset-password?email={Uri.EscapeDataString(toEmail)}&token={Uri.EscapeDataString(resetToken)}";

            var bodyBuilder = new BodyBuilder
            {
                TextBody =
                    $"Has solicitado restablecer tu contraseña.\n\n" +
                    $"Código de recuperación: {resetToken}\n\n" +
                    $"También puedes usar el siguiente enlace:\n{resetLink}\n\n" +
                    $"Si no solicitaste este cambio, puedes ignorar este mensaje."
            };

            message.Body = bodyBuilder.ToMessageBody();

            await SendAsync(message);
        }

        // Método interno reutilizable para enviar correos
        private async Task SendAsync(MimeMessage message)
        {
            using var client = new SmtpClient();

            try
            {
                await client.ConnectAsync(_smtpServer, _smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_smtpUsername, _smtpPassword);
                await client.SendAsync(message);
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}
