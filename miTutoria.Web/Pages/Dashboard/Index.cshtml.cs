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

    // Track 2: calendario mensual del aula (grilla por días, color por hijo, letra por tipo).
    public bool InboxEnabled { get; private set; }
    public int CalYear { get; private set; }
    public int CalMonth { get; private set; }
    public string CalMonthLabel { get; private set; } = string.Empty;   // "junio 2026"
    public string CalPrevMes { get; private set; } = string.Empty;      // yyyy-MM
    public string CalNextMes { get; private set; } = string.Empty;      // yyyy-MM
    public int CalDaysInMonth { get; private set; }
    public int CalFirstDowOffset { get; private set; }                  // 0=Lunes .. 6=Domingo
    public int CalTodayDay { get; private set; }                        // día de hoy si el mes mostrado es el actual (si no, 0)
    public Dictionary<int, List<CalEvent>> CalByDay { get; private set; } = new();
    public List<(int Id, string Name, string Color)> CalLegend { get; private set; } = new();
    public bool HasCalendar => CalByDay.Count > 0;
    public record CalEvent(int StudentId, string StudentName, string Color, string Letter, string Title, string CourseName);

    private static readonly string[] MesesEs =
        { "enero", "febrero", "marzo", "abril", "mayo", "junio",
          "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre" };

    // Letra del tipo de evento: E=Examen (inferido del título), T=Tarea, M=Material, A=Anuncio.
    private static string LetterFor(ClassroomItemType type, string title)
    {
        var t = (title ?? string.Empty).ToLowerInvariant();
        if (t.Contains("examen") || t.Contains("prueba") || t.Contains("evaluac") || t.Contains("parcial"))
            return "E";
        return type switch
        {
            ClassroomItemType.Material => "M",
            ClassroomItemType.Announcement => "A",
            _ => "T"
        };
    }

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

    public async Task<IActionResult> OnGetAsync(string? mes = null)
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
        // Los límites "hoy/semana/mes" se anclan a la medianoche ARGENTINA (UTC-3 fijo, sin DST),
        // no a la UTC: si no, lo que el chico estudia después de las 21:00 ART cae en "mañana".
        var argOffset = TimeSpan.FromHours(-3);
        var nowLocal  = now + argOffset;                                   // hora de pared argentina
        DateTime ToUtc(DateTime local) => DateTime.SpecifyKind(local - argOffset, DateTimeKind.Utc);
        var monthStart = ToUtc(new DateTime(nowLocal.Year, nowLocal.Month, 1));
        var weekStart  = ToUtc(nowLocal.Date.AddDays(-(int)nowLocal.DayOfWeek));
        var dayStart   = ToUtc(nowLocal.Date);
        DaysInMonth = DateTime.DaysInMonth(nowLocal.Year, nowLocal.Month);

        // Global (kill-switch) Y por-familia: el botón solo aparece para las familias habilitadas.
        MpEnabled = _config.GetValue("MP_ENABLED", false)
                    && !string.IsNullOrWhiteSpace(_config["MP_ACCESS_TOKEN"])
                    && family.PayEnabled;
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

        // Track 2: calendario mensual de entregas (gateado por flag global + familia).
        if (_config.GetValue("INBOX_FEATURE_ENABLED", false) && family.InboxEnabled)
        {
            InboxEnabled = true;

            // Mes mostrado: ?mes=yyyy-MM o el mes actual (ART).
            int calY = nowLocal.Year, calM = nowLocal.Month;
            if (!string.IsNullOrEmpty(mes) && DateTime.TryParseExact(mes + "-01", "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var md))
            { calY = md.Year; calM = md.Month; }

            var firstOfMonth = new DateTime(calY, calM, 1);
            CalYear = calY; CalMonth = calM;
            CalDaysInMonth = DateTime.DaysInMonth(calY, calM);
            CalFirstDowOffset = ((int)firstOfMonth.DayOfWeek + 6) % 7;   // Lunes=0
            CalMonthLabel = $"{MesesEs[calM - 1]} {calY}";
            CalPrevMes = firstOfMonth.AddMonths(-1).ToString("yyyy-MM");
            CalNextMes = firstOfMonth.AddMonths(1).ToString("yyyy-MM");
            CalTodayDay = (calY == nowLocal.Year && calM == nowLocal.Month) ? nowLocal.Day : 0;

            // Color por hijo, mismo criterio que el gráfico (índice en Students).
            var colorById = Students
                .Select((s, i) => (s.Id, Color: ChartColors[i % ChartColors.Length]))
                .ToDictionary(x => x.Id, x => x.Color);
            var nameById = Students.ToDictionary(s => s.Id, s => s.Nickname ?? s.FullName);

            var nextMonth = firstOfMonth.AddMonths(1);
            var calEvents = await _dbContext.DetectedAssignments
                .Where(d => studentIds.Contains(d.StudentId) && !d.Done
                            && d.DueDate != null && d.DueDate >= firstOfMonth && d.DueDate < nextMonth)
                .OrderBy(d => d.DueDate)
                .ToListAsync();

            var legend = new HashSet<int>();
            foreach (var d in calEvents)
            {
                var day = d.DueDate!.Value.Day;
                if (!CalByDay.TryGetValue(day, out var list)) { list = new(); CalByDay[day] = list; }
                var color = colorById.TryGetValue(d.StudentId, out var c) ? c : "#888";
                var name = nameById.TryGetValue(d.StudentId, out var n) ? n : "—";
                list.Add(new CalEvent(d.StudentId, name, color, LetterFor(d.Type, d.Title), d.Title, d.CourseName));
                legend.Add(d.StudentId);
            }
            CalLegend = Students.Where(s => legend.Contains(s.Id))
                .Select(s => (s.Id, Name: nameById[s.Id], Color: colorById[s.Id])).ToList();
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
