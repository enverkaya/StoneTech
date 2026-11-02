using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoneTech.Data;
using StoneTech.Models.ActivityLog;
using Microsoft.AspNetCore.Authorization;


namespace StoneTech.Controllers
{

    // İşlem geçmişine yalnızca Admin rolündeki kullanıcıların erişmesini sağlar
    [Authorize(Roles = "Admin")]
    public class ActivityLogController : Controller
    {
        private readonly AppDbContext _context;

        // Veritabanı bağlantısını controller'a alır
        public ActivityLogController(AppDbContext context)
        {
            _context = context;
        }

        // Sistem içerisinde gerçekleştirilen son işlemleri listeler
        public async Task<IActionResult> Index(
            string? search,
            int page = 1,
            int pageSize = 10)
        {
            if (page < 1)
                page = 1;

            int[] allowedPageSizes = { 5, 10, 20, 50 };

            if (!allowedPageSizes.Contains(pageSize))
                pageSize = 10;

            var query = _context.ActivityLogs
                .AsNoTracking()
                .AsQueryable();

            // Kullanıcının girdiği arama metnine göre işlem geçmişini filtreler
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    x.UserFullName.Contains(search) ||
                    x.Username.Contains(search) ||
                    x.Module.Contains(search) ||
                    x.Action.Contains(search) ||
                    x.Description.Contains(search));
            }

            int totalCount = await query.CountAsync();

            int totalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
                page = totalPages;

            var activityLogs = await query
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.ActivityLogId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ActivityLogListViewModel
                {
                    ActivityLogId = x.ActivityLogId,
                    UserFullName = x.UserFullName,
                    Username = x.Username,
                    Module = x.Module,
                    Action = x.Action,
                    RecordId = x.RecordId,
                    Description = x.Description,
                    CreatedDate = x.CreatedDate,
                    IpAddress = x.IpAddress
                })
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(activityLogs);
        }
    }
}