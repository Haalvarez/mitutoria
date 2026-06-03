using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;
using miTutoria.Web.Data.Entities.Auth;
using miTutoria.Web.Data.Entities.Billing;

namespace miTutoria.Web.Pages.Dashboard;

public class StudentSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ExchangesToday { get; set; }
    public int ExchangesThisWeek { get; set; }
    public int ExchangesThisMonth { get; set; }
    public int StreakDays { get; set; }
    public DateTime? LastActivity { get; set; }
    public Dictionary<int, int> ExchangesByDay { get; set; } = new();
}

public class IndexModel : PageModel
{
    private readonly AppDbContext _dbContext;

    public IndexModel(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public string FamilyName { get; private set; } = string.Empty;
    public List<User> Students { get; private set; } = new();
    public List<StudentSummary> StudentSummaries { get; private set; } = new();
    public int TotalExchangesMonth { get; private set; }
    public int ActiveDaysThisMonth { get; private set; }
    public int MaterialsCount { get; private set; }
    public int DaysInMonth { get; private set; }
    public string ChartJson { get; private set; } = "{}";

    private static readonly string[] ChartColors = { "#C94A1F", "#5C7A5E", "#A89880", "#4A7CA0", "#8A6A9A" };

    public async Task<IActionResult> OnGetAsync()
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return RedirectToPage("/Login");

        var family = await _dbContext.Families
            .Include(f => f.Users.Where(u => u.Role == UserRole.Student))
            .SingleOrDefaultAsync(f => f.Id == familyId.Value);

        if (family is null) return RedirectToPage("/Login");

        FamilyName = family.Nickname ?? family.Name ?? family.Email;
        Students = family.Users.OrderBy(u => u.FullName).ToList();

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var weekStart  = now.Date.AddDays(-(int)now.DayOfWeek).ToUniversalTime();
        var dayStart   = now.Date.ToUniversalTime();
        DaysInMonth = DateTime.DaysInMonth(now.Year, now.Month);

        var events = await _dbContext.TokenEvents
            .Where(t => t.FamilyId == familyId.Value && t.CreatedAt >= monthStart)
            .ToListAsync();

        // Para racha necesitamos historial completo por alumno
        var allChatDates = await _dbContext.TokenEvents
            .Where(t => t.FamilyId == familyId.Value && t.Feature == "chat" && t.UserId.HasValue)
            .Select(t => new { t.UserId, t.CreatedAt })
            .ToListAsync();

        TotalExchangesMonth = events.Count(t => t.Feature == "chat");
        ActiveDaysThisMonth = events.Where(t => t.Feature == "chat")
                                    .Select(t => t.CreatedAt.Date)
                                    .Distinct()
                                    .Count();

        var studentIds = Students.Select(s => s.Id).ToList();
        MaterialsCount = await _dbContext.Classrooms
            .CountAsync(c => studentIds.Contains(c.StudentId)
                          && c.Material != null && c.Material != "");

        foreach (var student in Students)
        {
            var mine = events.Where(t => t.UserId == student.Id).ToList();
            var chatMine = mine.Where(t => t.Feature == "chat").ToList();
            var allDates = allChatDates.Where(t => t.UserId == student.Id).Select(t => t.CreatedAt);

            StudentSummaries.Add(new StudentSummary
            {
                Id   = student.Id,
                Name = student.Nickname ?? student.FullName,
                ExchangesToday     = chatMine.Count(t => t.CreatedAt >= dayStart),
                ExchangesThisWeek  = chatMine.Count(t => t.CreatedAt >= weekStart),
                ExchangesThisMonth = chatMine.Count,
                StreakDays         = CalculateStreak(allDates),
                LastActivity       = mine.Any() ? mine.Max(t => t.CreatedAt) : null,
                ExchangesByDay     = chatMine
                    .GroupBy(t => t.CreatedAt.Day)
                    .ToDictionary(g => g.Key, g => g.Count())
            });
        }

        BuildChartJson();
        return Page();
    }

    private static int CalculateStreak(IEnumerable<DateTime> createdAts)
    {
        var today = DateTime.UtcNow.Date;
        var activeDays = createdAts.Select(d => d.Date).Distinct().OrderByDescending(d => d).ToList();
        if (activeDays.Count == 0) return 0;

        // Si hoy no hay actividad, la racha sigue viva desde ayer (igual que Duolingo)
        var start = activeDays.Contains(today) ? today : today.AddDays(-1);
        if (!activeDays.Contains(start)) return 0;

        int streak = 0;
        var expected = start;
        foreach (var day in activeDays.Where(d => d <= start).OrderByDescending(d => d))
        {
            if (day == expected) { streak++; expected = expected.AddDays(-1); }
            else break;
        }
        return streak;
    }

    private void BuildChartJson()
    {
        var labels = Enumerable.Range(1, DaysInMonth).Select(d => d.ToString()).ToArray();
        var datasets = StudentSummaries.Select((s, i) => new
        {
            label = s.Name,
            data  = Enumerable.Range(1, DaysInMonth)
                              .Select(d => s.ExchangesByDay.TryGetValue(d, out var v) ? v : 0)
                              .ToArray(),
            backgroundColor = ChartColors[i % ChartColors.Length] + "99",
            borderColor     = ChartColors[i % ChartColors.Length],
            borderWidth = 1,
            borderRadius = 2
        }).ToArray();

        ChartJson = JsonSerializer.Serialize(new { labels, datasets });
    }
}
