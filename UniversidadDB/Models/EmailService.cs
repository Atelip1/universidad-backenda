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
