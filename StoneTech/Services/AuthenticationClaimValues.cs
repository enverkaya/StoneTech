using System.Security.Cryptography;
using System.Text;

namespace StoneTech.Services
{
    // Oturum güvenliði için kullanýlan claim deðerlerini oluþturur
    public static class AuthenticationClaimValues
    {
        // Þifre sürümünü cookie içinde tutacak claim adý
        public const string PasswordVersionClaimType = "PasswordVersion";

        // Mevcut þifre hash'inden güvenli bir sürüm deðeri oluþturur
        public static string CreatePasswordVersion(string passwordHash)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(passwordHash);

            // SHA256 ile karþýlaþtýrýlabilir sabit bir deðer üretir
            return Convert.ToHexString(SHA256.HashData(bytes));
        }
    }
}