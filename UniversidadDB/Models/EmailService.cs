using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace UniversidadDB.Models
{
    public class EmailService
    {
        private readonly string _smtpServer = "smtp.gmail.com";               // Servidor SMTP de Gmail
        private readonly int _smtpPort = 587;                                  // Puerto SMTP para TLS
        private readonly string _smtpUsername = "mariadelpilartasaycolaque@gmail.com";  // Tu correo de Gmail
        private readonly string _smtpPassword = "snmj mwjx cxja zcaj";         // Contraseña de aplicación

        // =============== Correo de bienvenida ===============
        public async Task SendWelcomeEmail(string toEmail, string userName)
        {
            var subject = "Bienvenido a la Universidad";
            var body =
                $"Hola {userName},\n\n" +
                "Gracias por registrarte en la Universidad.\n\n" +
                "Atentamente,\n" +
                "El equipo de la universidad.";

            await SendEmailAsync(toEmail, subject, body);
        }

        // =============== Correo de recuperación de contraseña ===============
        public async Task SendPasswordResetEmail(string toEmail, string code)
        {
            var subject = "Recuperación de contraseña - Portal Académico UPSJB";
            var body =
                "Hola,\n\n" +
                "Hemos recibido una solicitud para restablecer tu contraseña del Portal Académico UPSJB.\n\n" +
                $"Tu código de verificación es: {code}\n\n" +
                "Este código es válido por 15 minutos. " +
                "Si no solicitaste este cambio, puedes ignorar este correo.\n\n" +
                "Saludos,\n" +
                "Portal Académico UPSJB";

            await SendEmailAsync(toEmail, subject, body);
        }

        // =============== Método común para enviar correos ===============
        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var message = new MimeMessage();

            // Remitente y destinatario
            message.From.Add(new MailboxAddress("Universidad", "no-reply@upsjb.edu.pe"));
            message.To.Add(new MailboxAddress(toEmail, toEmail));

            message.Subject = subject;
            message.Body = new TextPart("plain")
            {
                Text = body
            };

            try
            {
                using (var client = new SmtpClient())
                {
                    // Conectar con el servidor SMTP de Gmail usando TLS
                    await client.ConnectAsync(_smtpServer, _smtpPort, SecureSocketOptions.StartTls);

                    // Autenticación con tu cuenta de Gmail
                    await client.AuthenticateAsync(_smtpUsername, _smtpPassword);

                    // Enviar el mensaje
                    await client.SendAsync(message);

                    // Desconectar del servidor SMTP
                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                // Log y re-lanzamos para que el controlador pueda manejar el error
                Console.WriteLine($"Error al enviar el correo: {ex.Message}");
                throw;
            }
        }
    }
}
