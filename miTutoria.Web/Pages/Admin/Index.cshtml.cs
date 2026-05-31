using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;

namespace miTutoria.Web.Pages.Admin;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public IndexModel(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // ── Familias ─────────────────────────────────────────────────────────────

    public record FamilyRow(
        int Id, string Name,
        int Students,
        int ExchangesToday, int ExchangesWeek, int ExchangesMonth,
        decimal CostUsdMonth, decimal CostArsMonth,
        DateTime? LastActivity,
        bool NearLimit, bool Inactive7Days, bool StudentsWithoutMaterial);

    public List<FamilyRow> Families { get; private set; } = [];

    // ── Waitlist ─────────────────────────────────────────────────────────────

    public record WaitlistRow(string Email, string? Name, DateTime CreatedAt);
    public List<WaitlistRow> Waitlist { get; private set; } = [];

    // ── Globales ─────────────────────────────────────────────────────────────

    public decimal TotalCostUsdMonth { get; private set; }
    public decimal TotalCostArsMonth { get; private set; }
    public Dictionary<string, long> TokensByFeature { get; private set; } = [];
    public long MonthlyTokenLimit { get; private set; }

    // ── GET ──────────────────────────────────────────────────────────────────

    public async Task<IActionResult> OnGetAsync([FromQuery] string? token)
    {
        var adminToken = _config["ADMIN_TOKEN"];
        if (string.IsNullOrWhiteSpace(adminToken) || token != adminToken)
            return Unauthorized();

        var now = DateTime.UtcNow;
        var todayStart   = now.Date;
        var weekStart    = todayStart.AddDays(-6);
        var monthStart   = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        MonthlyTokenLimit = _config.GetValue<long>("MONTHLY_TOKEN_LIMIT", 500_000);

        // Todas las familias con sus usuarios
        var families = await _db.Families
            .Include(f => f.Users.Where(u => u.Role == Data.Entities.Auth.UserRole.Student))
            .ToListAsync();

        // Token events del mes
        var events = await _db.TokenEvents
            .Where(t => t.CreatedAt >= monthStart)
            .ToListAsync();

        // Classrooms con material
        var classrooms = await _db.Classrooms.ToListAsync();

        Families = families.Select(f =>
        {
            var fEvents = events.Where(e => e.FamilyId == f.Id).ToList();
            var today   = fEvents.Where(e => e.CreatedAt >= todayStart).ToList();
            var week    = fEvents.Where(e => e.CreatedAt >= weekStart).ToList();

            var chatToday = today.Count(e => e.Feature == "chat");
            var chatWeek  = week.Count(e => e.Feature == "chat");
            var chatMonth = fEvents.Count(e => e.Feature == "chat");

            var costUsd = fEvents.Sum(e => e.CostUsd);
            var costArs = fEvents.Where(e => e.ArsRate.HasValue)
                                 .Sum(e => e.CostUsd * e.ArsRate!.Value);

            var lastActivity = fEvents.Any() ? fEvents.Max(e => e.CreatedAt) : (DateTime?)null;
            var totalTokens  = fEvents.Sum(e => (long)e.TokensIn + e.TokensOut);
            var stuIds       = f.Users.Select(u => u.Id).ToHashSet();
            var studentsWithoutMaterial = f.Users.Any(u =>
                classrooms.Any(c => c.StudentId == u.Id && string.IsNullOrWhiteSpace(c.Material)));

            return new FamilyRow(
                f.Id,
                f.Nickname ?? f.Email,
                f.Users.Count,
                chatToday, chatWeek, chatMonth,
                costUsd, costArs,
                lastActivity,
                NearLimit: totalTokens > MonthlyTokenLimit * 0.8m,
                Inactive7Days: lastActivity.HasValue && (now - lastActivity.Value).TotalDays > 7,
                StudentsWithoutMaterial: studentsWithoutMaterial
            );
        }).OrderByDescending(f => f.LastActivity).ToList();

        // Waitlist
        Waitlist = await _db.WaitlistEntries
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WaitlistRow(w.Email, w.Name, w.CreatedAt))
            .ToListAsync();

        // Globales
        TotalCostUsdMonth = events.Sum(e => e.CostUsd);
        TotalCostArsMonth = events.Where(e => e.ArsRate.HasValue)
                                  .Sum(e => e.CostUsd * e.ArsRate!.Value);
        TokensByFeature = events
            .GroupBy(e => e.Feature)
            .ToDictionary(g => g.Key, g => g.Sum(e => (long)e.TokensIn + e.TokensOut));

        return Page();
    }
}
