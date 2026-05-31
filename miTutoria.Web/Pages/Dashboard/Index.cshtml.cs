using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;
using miTutoria.Web.Data.Entities.Auth;

namespace miTutoria.Web.Pages.Dashboard;

public class StudentUsage
{
    public string Name { get; set; } = string.Empty;
    public long TotalTokens { get; set; }
    public decimal TotalCostUsd { get; set; }
    public Dictionary<int, long> TokensByDay { get; set; } = new();
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
    public List<StudentUsage> UsageByStudent { get; private set; } = new();
    public long TotalTokensMonth { get; private set; }
    public decimal TotalCostMonth { get; private set; }
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
        DaysInMonth = DateTime.DaysInMonth(now.Year, now.Month);

        var events = await _dbContext.TokenEvents
            .Where(t => t.FamilyId == familyId.Value && t.CreatedAt >= monthStart)
            .ToListAsync();

        TotalTokensMonth = events.Sum(t => (long)t.TokensIn + t.TokensOut);
        TotalCostMonth = events.Sum(t => t.CostUsd);

        var studentIds = Students.Select(s => s.Id).ToHashSet();
        var byStudent = events
            .Where(t => t.UserId.HasValue && studentIds.Contains(t.UserId.Value))
            .GroupBy(t => t.UserId!.Value);

        var studentMap = Students.ToDictionary(s => s.Id);
        foreach (var group in byStudent)
        {
            var student = studentMap[group.Key];
            UsageByStudent.Add(new StudentUsage
            {
                Name = student.Nickname ?? student.FullName,
                TotalTokens = group.Sum(t => (long)t.TokensIn + t.TokensOut),
                TotalCostUsd = group.Sum(t => t.CostUsd),
                TokensByDay = group
                    .GroupBy(t => t.CreatedAt.Day)
                    .ToDictionary(g => g.Key, g => g.Sum(t => (long)t.TokensIn + t.TokensOut))
            });
        }

        BuildChartJson();
        return Page();
    }

    private void BuildChartJson()
    {
        var labels = Enumerable.Range(1, DaysInMonth).Select(d => d.ToString()).ToArray();
        var datasets = UsageByStudent.Select((u, i) => new
        {
            label = u.Name,
            data = Enumerable.Range(1, DaysInMonth)
                             .Select(d => u.TokensByDay.TryGetValue(d, out var v) ? v : 0L)
                             .ToArray(),
            backgroundColor = ChartColors[i % ChartColors.Length] + "99",
            borderColor = ChartColors[i % ChartColors.Length],
            borderWidth = 1,
            borderRadius = 2
        }).ToArray();

        ChartJson = JsonSerializer.Serialize(new { labels, datasets });
    }
}
