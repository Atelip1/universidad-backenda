using MailKit.Net.Smtp;
using MimeKit;
using System.Threading.Tasks;
using MailKit.Security;
public class EmailService
{
    private readonly string _smtpServer = "smtp.gmail.com"; // Cambia esto por tu servidor SMTP
    private readonly int _smtpPort = 587; // Usa el puerto adecuado
    private readonly string _smtpUsername = "mariadelpilartasaycolaque@gmail.com";
    private readonly string _smtpPassword = "snmj mwjx cxja zcaj";

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
            await client.ConnectAsync(_smtpServer, _smtpPort, false);
            await client.AuthenticateAsync(_smtpUsername, _smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
    catch (Exception ex)
        {
            // Maneja el error, por ejemplo, logueo
            Console.WriteLine($"Error al enviar el correo: {ex.Message}");
        }
    }
}