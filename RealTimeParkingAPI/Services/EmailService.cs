using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace RealTimeParkingAPI.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
            Console.WriteLine("SMTP HOST: " + _settings.SmtpHost);
        }

        public async Task SendEmailAsync(string toEmail, string subject, string bodyHtml)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = bodyHtml
            };

            message.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();

            smtp.CheckCertificateRevocation = false;

            await smtp.ConnectAsync(
                _settings.SmtpHost,
                _settings.SmtpPort,
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(_settings.Username, _settings.AppPassword);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}