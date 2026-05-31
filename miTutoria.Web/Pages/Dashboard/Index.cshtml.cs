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
    public decimal TotalCostArs { get; private set; }
    public int TotalExchangesMonth { get; private set; }
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

        TotalCostArs = events.Sum(t => t.CostUsd * (t.ArsRate ?? 0m));
        TotalExchangesMonth = events.Count(t => t.Feature == "chat");

        var studentIds = Students.Select(s => s.Id).ToHashSet();
        var studentMap = Students.ToDictionary(s => s.Id);

        foreach (var student in Students)
        {
            var mine = events.Where(t => t.UserId == student.Id).ToList();
            var chatMine = mine.Where(t => t.Feature == "chat").ToList();

            StudentSummaries.Add(new StudentSummary
            {
                Id   = student.Id,
                Name = student.Nickname ?? student.FullName,
                ExchangesToday     = chatMine.Count(t => t.CreatedAt >= dayStart),
                ExchangesThisWeek  = chatMine.Count(t => t.CreatedAt >= weekStart),
                ExchangesThisMonth = chatMine.Count,
                LastActivity       = mine.Any() ? mine.Max(t => t.CreatedAt) : null,
                ExchangesByDay     = chatMine
                    .GroupBy(t => t.CreatedAt.Day)
                    .ToDictionary(g => g.Key, g => g.Count())
            });
        }

        BuildChartJson();
        return Page();
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
