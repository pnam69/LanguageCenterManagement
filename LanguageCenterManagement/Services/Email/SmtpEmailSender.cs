using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace LanguageCenterManagement.Services.Email
{
    public class SmtpEmailSender : IAppEmailSender
    {
        private readonly IConfiguration _configuration;

        public SmtpEmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendAsync(
            string email,
            string subject,
            string htmlMessage)
        {
            var settings = _configuration.GetSection("EmailSettings");

            var host = settings["SmtpServer"]
                ?? throw new InvalidOperationException(
                    "Email SMTP server is not configured.");

            var port = int.Parse(
                settings["Port"] ?? "587");

            var username = settings["Username"]
                ?? throw new InvalidOperationException(
                    "Email username is not configured.");

            var password = settings["Password"]
                ?? throw new InvalidOperationException(
                    "Email password is not configured.");

            var senderName = settings["SenderName"]
                ?? "Language Center";

            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(senderName, username));

            message.To.Add(
                MailboxAddress.Parse(email));

            message.Subject = subject;

            message.Body = new BodyBuilder
            {
                HtmlBody = htmlMessage
            }.ToMessageBody();

            using var client = new SmtpClient();

            await client.ConnectAsync(
                host,
                port,
                SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                username,
                password);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
