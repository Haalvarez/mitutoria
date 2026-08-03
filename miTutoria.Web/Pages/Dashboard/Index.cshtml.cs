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

    // Calendario público compartible: token (null = no compartido) + visitas acumuladas.
    public string? ShareToken { get; set; }
    public int ShareViews { get; set; }
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

    // Track 2: calendario del aula como ventana móvil (10 atrás / 20 adelante), color por hijo.
    public bool InboxEnabled { get; private set; }
    public string CalRangeLabel { get; private set; } = string.Empty;
    public int CalPrevOff { get; private set; }
    public int CalNextOff { get; private set; }
    public List<CalDayVM> CalDays { get; private set; } = new();
    public List<CalEvent> UndatedUpcoming { get; private set; } = new();   // entregas sin fecha parseada
    public List<(int Id, string Name, string Color)> CalLegend { get; private set; } = new();
    public bool HasCalendar => CalDays.Any(d => d.Events.Count > 0);
    public record CalEvent(int StudentId, string StudentName, string Color, string Letter, string Title, string CourseName);
    public record CalDayVM(int Index, int DayNum, string? MonthAbbr, bool IsToday, bool IsPast, List<CalEvent> Events);

    public string FamilyName { get; private set; } = string.Empty;
    public string SubscriptionStatus { get; private set; } = "trial";
    public DateTime? AccessEndsAt { get; private set; }   // PaidUntil ?? TrialEndsAt — sirve para fin de trial y renovación mensual
    public int DaysRemaining { get; private set; }
    public bool AccessExpired { get; private set; }        // el vencimiento ya pasó (venció, no "vence hoy")
    public bool WeeklyDigestOptIn { get; private set; }   // resumen semanal por mail (viernes 20hs)
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
    // Base URL para armar el link público compartible (mitutoria.app/agenda/...).
    public string BaseUrl { get; private set; } = string.Empty;
    // Feedback del retorno de pago (?pago=ok|error|pendiente|mail).
    public string? PagoResult { get; set; }

    private static readonly string[] ChartColors = { "#C94A1F", "#5C7A5E", "#A89880", "#4A7CA0", "#8A6A9A" };

    public async Task<IActionResult> OnGetAsync(int off = 0)
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return RedirectToPage("/Login");

        var family = await _dbContext.Families
            .Include(f => f.Users.Where(u => u.Role == UserRole.Student))
            .SingleOrDefaultAsync(f => f.Id == familyId.Value);

        if (family is null) return RedirectToPage("/Login");

        FamilyName = family.Nickname ?? family.Name ?? family.Email;
        BaseUrl = _config["APP_BASE_URL"] ?? $"{Request.Scheme}://{Request.Host}";
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
        WeeklyDigestOptIn = family.WeeklyDigestOptIn;
        AccessEndsAt = family.PaidUntil ?? family.TrialEndsAt;
        // Comparamos por FECHA de pared argentina (no por instante UTC) para no confundir
        // "vence hoy" con "venció": una fecha pasada NO debe colapsar a 0 y leerse como hoy.
        if (AccessEndsAt.HasValue)
        {
            var endLocalDate = (AccessEndsAt.Value + argOffset).Date;   // día AR del vencimiento
            var todayLocal   = nowLocal.Date;
            AccessExpired = endLocalDate < todayLocal;
            DaysRemaining = Math.Max(0, (endLocalDate - todayLocal).Days);
        }

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

        // Visitas del calendario público compartido, por hijo.
        var shareViews = await _dbContext.AgendaViews
            .Where(v => studentIds.Contains(v.StudentId))
            .GroupBy(v => v.StudentId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

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
                InterruptionsToday  = att.interruptions,
                ShareToken          = student.AgendaShareToken,
                ShareViews          = shareViews.GetValueOrDefault(student.Id)
            });
        }

        // Track 2: calendario como ventana móvil (gateado por flag global + familia).
        if (_config.GetValue("INBOX_FEATURE_ENABLED", false) && family.InboxEnabled)
        {
            InboxEnabled = true;
            var (slots, fromUtc, toUtc, rangeLabel, prevOff, nextOff) =
                Infrastructure.AgendaWindow.Build(now, Infrastructure.AgendaWindow.DefaultBack,
                                                  Infrastructure.AgendaWindow.DefaultFwd, off);
            CalRangeLabel = rangeLabel; CalPrevOff = prevOff; CalNextOff = nextOff;

            // Color por hijo, mismo criterio que el gráfico (índice en Students).
            var colorById = Students
                .Select((s, i) => (s.Id, Color: ChartColors[i % ChartColors.Length]))
                .ToDictionary(x => x.Id, x => x.Color);
            var nameById = Students.ToDictionary(s => s.Id, s => s.Nickname ?? s.FullName);

            var calEvents = await _dbContext.DetectedAssignments
                .Where(d => studentIds.Contains(d.StudentId) && !d.Done
                            && d.DueDate != null && d.DueDate >= fromUtc && d.DueDate < toUtc)
                .ToListAsync();

            var byDate = new Dictionary<DateTime, List<CalEvent>>();
            var legend = new HashSet<int>();
            foreach (var d in calEvents)
            {
                var date = d.DueDate!.Value.Date;
                if (!byDate.TryGetValue(date, out var list)) { list = new(); byDate[date] = list; }
                var color = colorById.TryGetValue(d.StudentId, out var c) ? c : "#888";
                var name = nameById.TryGetValue(d.StudentId, out var n) ? n : "—";
                list.Add(new CalEvent(d.StudentId, name, color, Infrastructure.AgendaWindow.LetterFor(d.Type, d.Title), d.Title, d.CourseName));
                legend.Add(d.StudentId);
            }
            CalDays = slots.Select(sl => new CalDayVM(sl.Index, sl.DayNum, sl.MonthAbbr, sl.IsToday, sl.IsPast,
                byDate.GetValueOrDefault(sl.Date.Date) ?? new())).ToList();

            // Entregas sin fecha parseada (tarea/recordatorio): no caen en un día pero importan.
            var undated = await _dbContext.DetectedAssignments
                .Where(d => studentIds.Contains(d.StudentId) && !d.Done && d.DueDate == null
                            && (d.Type == ClassroomItemType.Assignment || d.Type == ClassroomItemType.DueReminder))
                .OrderByDescending(d => d.MessageDate)
                .Take(30)
                .ToListAsync();
            foreach (var d in undated)
            {
                var color = colorById.TryGetValue(d.StudentId, out var c) ? c : "#888";
                var name = nameById.TryGetValue(d.StudentId, out var n) ? n : "—";
                UndatedUpcoming.Add(new CalEvent(d.StudentId, name, color,
                    Infrastructure.AgendaWindow.LetterFor(d.Type, d.Title), d.Title, d.CourseName));
                legend.Add(d.StudentId);
            }

            CalLegend = Students.Where(s => legend.Contains(s.Id))
                .Select(s => (s.Id, Name: nameById[s.Id], Color: colorById[s.Id])).ToList();
        }

        BuildChartJson();
        return Page();
    }

    // Genera (una vez) el token del calendario público de un hijo. Opt-in del padre.
    public async Task<IActionResult> OnPostShareCalendarAsync(int studentId)
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return RedirectToPage("/Login");

        var student = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == studentId && u.FamilyId == familyId.Value);
        if (student is not null && string.IsNullOrEmpty(student.AgendaShareToken))
        {
            student.AgendaShareToken = Guid.NewGuid().ToString("N");
            await _dbContext.SaveChangesAsync();
        }
        return RedirectToPage("/Dashboard/Index");
    }

    // El padre activa/desactiva el resumen semanal por mail (viernes 20hs ARG).
    public async Task<IActionResult> OnPostToggleDigestAsync(bool optIn)
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return RedirectToPage("/Login");

        var family = await _dbContext.Families.FirstOrDefaultAsync(f => f.Id == familyId.Value);
        if (family is not null)
        {
            family.WeeklyDigestOptIn = optIn;
            await _dbContext.SaveChangesAsync();
        }
        return RedirectToPage("/Dashboard/Index");
    }

    private static int CalculateStreak(IEnumerable<DateTime> createdAts)
    {
        // Día en hora argentina (UTC-3): la racha no debe cortarse a la medianoche UTC (21hs ARG).
        var arg = TimeSpan.FromHours(-3);
        var today = (DateTime.UtcNow + arg).Date;
        var activeDays = createdAts.Select(d => (d + arg).Date).Distinct().OrderByDescending(d => d).ToList();
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
