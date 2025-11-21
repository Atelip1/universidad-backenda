using MailKit.Net.Smtp;
using MimeKit;
using System.Threading.Tasks;
using MailKit.Security;
using System;

public class EmailService
{
    private readonly string _smtpServer = "smtp.gmail.com"; // Servidor SMTP de Gmail
    private readonly int _smtpPort = 587; // Puerto SMTP para TLS
    private readonly string _smtpUsername = "mariadelpilartasaycolaque@gmail.com"; // Tu correo de Gmail
    private readonly string _smtpPassword = "snmj mwjx cxja zcaj"; // Utiliza una contraseña de aplicación si tienes habilitada la autenticación en dos pasos

    public async Task SendWelcomeEmail(string toEmail, string userName)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Universidad", "no-reply@upsjb.edu.pe"));
        message.To.Add(new MailboxAddress(userName, toEmail));
        message.Subject = "Bienvenido a la Universidad";

        var body = new TextPart("plain")
        {
            Text = $"Hola {userName},\n\nGracias por registrarte en la Universidad.\n\nAtentamente,\nEl equipo de la universidad."
        };

        message.Body = body;

        try
        {
            using (var client = new SmtpClient())
            {
                // Conectar con el servidor SMTP de Gmail usando TLS (true = TLS, false = no)
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
            // Manejo del error
            Console.WriteLine($"Error al enviar el correo: {ex.Message}");
        }
    }
}
