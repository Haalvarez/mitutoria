using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;
using miTutoria.Web.Data.Entities.Auth;
using miTutoria.Web.Data.Entities.Billing;
using miTutoria.Web.Inbox;

namespace miTutoria.Web.Pages.Dashboard;

public class StudentSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ExchangesToday { get; set; }
    public int ExchangesThisWeek { get; set; }
    public int ExchangesThisMonth { get; set; }
    public int StreakDays { get; set; }
    public string? Avatar { get; set; }
    public DateTime? LastActivity { get; set; }
    public Dictionary<int, int> ExchangesByDay { get; set; } = new();
    public int[] Last15Days { get; set; } = new int[15];

    // Atención dedicada (hoy): tiempo concentrado y % vs ausente. Solo lo ve el padre.
    public bool HasAttentionToday { get; set; }
    public int FocusedMinutesToday { get; set; }
    public int AttentionPct { get; set; }       // concentrado / (concentrado + ausente)
    public int InterruptionsToday { get; set; } // veces que se fue de la pestaña
}

public class IndexModel : PageModel
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _config;

    public IndexModel(AppDbContext dbContext, IConfiguration config)
    {
        _dbContext = dbContext;
        _config = config;
    }

    // Track 2: calendario del aula (próximas entregas de todos los hijos).
    public bool InboxEnabled { get; private set; }
    public List<CalendarEvent> Calendar { get; private set; } = new();
    public record CalendarEvent(string StudentName, ClassroomItemType Type, string Title,
        string CourseName, DateTime? DueDate, string? DueDateRaw, bool Done);

    public string FamilyName { get; private set; } = string.Empty;
    public string SubscriptionStatus { get; private set; } = "trial";
    public DateTime? AccessEndsAt { get; private set; }   // PaidUntil ?? TrialEndsAt — sirve para fin de trial y renovación mensual
    public int DaysRemaining { get; private set; }
    public List<User> Students { get; private set; } = new();
    public List<StudentSummary> StudentSummaries { get; private set; } = new();
    public int TotalExchangesMonth { get; private set; }
    public int ActiveDaysThisMonth { get; private set; }
    public int MaterialsCount { get; private set; }
    public int DaysInMonth { get; private set; }
    public string ChartJson { get; private set; } = "{}";

    // Cobro: habilita el botón "Quiero pagar" solo si MercadoPago está activo.
    public bool MpEnabled { get; private set; }
    // Atención dedicada: muestra la concentración por hijo si está activa.
    public bool AttentionEnabled { get; private set; }
    // Feedback del retorno de pago (?pago=ok|error|pendiente|mail).
    public string? PagoResult { get; set; }

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

        MpEnabled = _config.GetValue("MP_ENABLED", false)
                    && !string.IsNullOrWhiteSpace(_config["MP_ACCESS_TOKEN"]);
        PagoResult = Request.Query["pago"].FirstOrDefault();

        SubscriptionStatus = family.SubscriptionStatus;
        AccessEndsAt = family.PaidUntil ?? family.TrialEndsAt;
        DaysRemaining = AccessEndsAt.HasValue
            ? Math.Max(0, (int)Math.Ceiling((AccessEndsAt.Value - now).TotalDays))
            : 0;

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

        // Atención dedicada de HOY, agregada por hijo (suma de sus sesiones de foco del día).
        AttentionEnabled = _config.GetValue("ATTENTION_ENABLED", true);
        var attentionByStudent = new Dictionary<int, (long focused, long away, int interruptions)>();
        if (AttentionEnabled)
        {
            var todays = await _dbContext.FocusSessions
                .Where(f => studentIds.Contains(f.StudentId) && f.StartedAt >= dayStart)
                .GroupBy(f => f.StudentId)
                .Select(g => new
                {
                    StudentId = g.Key,
                    Focused = g.Sum(x => x.FocusedMs),
                    Away = g.Sum(x => x.AwayMs),
                    Interruptions = g.Sum(x => x.Interruptions)
                })
                .ToListAsync();
            foreach (var a in todays)
                attentionByStudent[a.StudentId] = (a.Focused, a.Away, a.Interruptions);
        }

        foreach (var student in Students)
        {
            var mine = events.Where(t => t.UserId == student.Id).ToList();
            var chatMine = mine.Where(t => t.Feature == "chat").ToList();
            var myDates = allChatDates.Where(t => t.UserId == student.Id).Select(t => t.CreatedAt).ToList();
            var allDates = myDates;

            // Últimos 15 días (día -14 .. hoy), cuenta de intercambios por día
            var last15 = Enumerable.Range(0, 15)
                .Select(i => myDates.Count(d => d.Date == now.Date.AddDays(-14 + i)))
                .ToArray();

            attentionByStudent.TryGetValue(student.Id, out var att);
            var present = att.focused + att.away;
            var hasAttention = present > 0 || att.focused > 0;

            StudentSummaries.Add(new StudentSummary
            {
                Id   = student.Id,
                Name = student.Nickname ?? student.FullName,
                Avatar = student.Avatar,
                ExchangesToday     = chatMine.Count(t => t.CreatedAt >= dayStart),
                ExchangesThisWeek  = chatMine.Count(t => t.CreatedAt >= weekStart),
                ExchangesThisMonth = chatMine.Count,
                StreakDays         = CalculateStreak(allDates),
                LastActivity       = mine.Any() ? mine.Max(t => t.CreatedAt) : null,
                ExchangesByDay     = chatMine
                    .GroupBy(t => t.CreatedAt.Day)
                    .ToDictionary(g => g.Key, g => g.Count()),
                Last15Days         = last15,
                HasAttentionToday   = hasAttention,
                FocusedMinutesToday = (int)Math.Round(att.focused / 60000.0),
                AttentionPct        = present > 0 ? (int)Math.Round(att.focused * 100.0 / present) : 0,
                InterruptionsToday  = att.interruptions
            });
        }

        // Track 2: calendario de próximas entregas (gateado por flag global + familia).
        if (_config.GetValue("INBOX_FEATURE_ENABLED", false) && family.InboxEnabled)
        {
            InboxEnabled = true;
            var since = now.Date.AddDays(-3);
            var nameById = Students.ToDictionary(s => s.Id, s => s.Nickname ?? s.FullName);
            var upcoming = await _dbContext.DetectedAssignments
                .Where(d => studentIds.Contains(d.StudentId) && d.DueDate != null && d.DueDate >= since && !d.Done)
                .OrderBy(d => d.DueDate)
                .Take(40)
                .ToListAsync();
            Calendar = upcoming.Select(d => new CalendarEvent(
                nameById.TryGetValue(d.StudentId, out var n) ? n : "—",
                d.Type, d.Title, d.CourseName, d.DueDate, d.DueDateRaw, d.Done)).ToList();
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
