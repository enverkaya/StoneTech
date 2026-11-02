
namespace StoneTech.Services
{
    public interface IMailService
    {
        // Belirtilen e-posta adresine mesaj gönderir
        Task SendEmailAsync(
            string toEmail,
            string subject,
            string message);
    }
}
