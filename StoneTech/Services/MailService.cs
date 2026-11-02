
using DocumentFormat.OpenXml.Vml;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Net.Mail;

namespace StoneTech.Services
{
    public class MailService : IMailService
    {
        // Uygulamanın SMTP ayarlarını okur
        private readonly IConfiguration _configuration;

        // Constructor
        public MailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Belirtilen e-posta adresine mesaj gönderir
        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string message)
        {
            // SMTP sunucusunu al
            string host = _configuration["MailSettings:Host"]
                ?? throw new InvalidOperationException(
                    "SMTP sunucusu tanımlanmamış.");

            // SMTP portunu al
            if (!int.TryParse(
                _configuration["MailSettings:Port"],
                out int port))
            {
                throw new InvalidOperationException(
                    "SMTP portu geçersiz.");
            }

            // SMTP kullanıcı adını al
            string username = _configuration["MailSettings:Username"]
                ?? throw new InvalidOperationException(
                    "SMTP kullanıcı adı tanımlanmamış.");

            // SMTP uygulama şifresini al
            string password = _configuration["MailSettings:Password"]
                ?? throw new InvalidOperationException(
                    "SMTP şifresi tanımlanmamış.");

            // Gönderen e-posta adresini al
            string fromEmail = _configuration["MailSettings:FromEmail"]
                ?? throw new InvalidOperationException(
                    "Gönderen e-posta adresi tanımlanmamış.");

            // Gönderen firma adını al
            string fromName = _configuration["MailSettings:FromName"]
                ?? "KAYA Ceramic & Tile";

            // Gönderilecek e-postayı oluştur
            var email = new MimeMessage();

            // Gönderen bilgilerini ekle
            email.From.Add(
                new MailboxAddress(fromName, fromEmail));

            // Alıcı e-posta adresini ekle
            email.To.Add(MailboxAddress.Parse(toEmail));

            // E-posta konusunu belirle
            email.Subject = subject;

            // E-posta içeriğini düz metin olarak oluştur
            email.Body = new TextPart("plain")
            {
                Text = message
            };

            // SMTP istemcisini oluştur
            using var smtp = new MailKit.Net.Smtp.SmtpClient();

            // SMTP sunucusuna TLS bağlantısı kur
            await smtp.ConnectAsync(
                host,
                port,
                SecureSocketOptions.StartTls);

            // E-posta hesabıyla kimlik doğrulaması yap
            await smtp.AuthenticateAsync(
                username,
                password);

            // E-postayı gönder
            await smtp.SendAsync(email);

            // SMTP bağlantısını kapat
            await smtp.DisconnectAsync(true);
        }
    }
}
