using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using StoneTech.Data;
using StoneTech.Entities;

namespace StoneTech.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityLogService(
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(
            string module,
            string action,
            int? recordId,
            string description)
        {
            // O anki HTTP isteğine erişir
            var httpContext = _httpContextAccessor.HttpContext;

            // Giriş yapan kullanıcının claim bilgilerine erişir
            var principal = httpContext?.User;

            // Kullanıcının UserId bilgisini claim içerisinden alır
            int? userId = null;

            var userIdValue = principal?.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (int.TryParse(userIdValue, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            // Kullanıcının görünen adını claim içerisinden alır
            var userFullName =
                principal?.FindFirstValue(ClaimTypes.Name)
                ?? "Sistem";

            // Kullanıcı adını login sırasında oluşturduğumuz Username claim'inden alır
            var username =
                principal?.FindFirstValue("Username")
                ?? "system";

            // İsteğin geldiği IP adresini alır
            var ipAddress =
                httpContext?.Connection.RemoteIpAddress?.ToString();

            // Yeni işlem geçmişi kaydını oluşturur
            var activityLog = new ActivityLog
            {
                UserId = userId,
                UserFullName = userFullName,
                Username = username,
                Module = module,
                Action = action,
                RecordId = recordId,
                Description = description,
                CreatedDate = DateTime.Now,
                IpAddress = ipAddress
            };

            // İşlem geçmişini veritabanına ekler
            _context.ActivityLogs.Add(activityLog);

            // Değişikliği veritabanına kaydeder
            await _context.SaveChangesAsync();
        }
    }
}