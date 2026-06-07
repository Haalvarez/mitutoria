using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;
using miTutoria.Web.Data.Entities.Academic;
using miTutoria.Web.Inbox;

namespace miTutoria.Web.Pages.Agenda;

/// <summary>
/// Calendario PÚBLICO y anónimo de una agenda compartida (token por alumno).
/// Anonimizado: materia + tipo + fecha, SIN nombre del hijo (es agenda de nivel clase).
/// Marketing encubierto: branding + CTA a mitutoria.app. Registra cada visita (analítica first-party).
/// </summary>
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public string ShareToken { get; private set; } = string.Empty;
    public int CalYear { get; private set; }
    public int CalMonth { get; private set; }
    public string CalMonthLabel { get; private set; } = string.Empty;
    public string CalPrevMes { get; private set; } = string.Empty;
    public string CalNextMes { get; private set; } = string.Empty;
    public int CalDaysInMonth { get; private set; }
    public int CalFirstDowOffset { get; private set; }
    public int CalTodayDay { get; private set; }
    public Dictionary<int, List<Cell>> CalByDay { get; private set; } = new();
    public List<(string Name, string Color)> CalLegend { get; private set; } = new();
    public bool HasCalendar => CalByDay.Count > 0;
    public record Cell(string Color, string Letter, string Title, string CourseName);

    private static readonly string[] MesesEs =
        { "enero", "febrero", "marzo", "abril", "mayo", "junio",
          "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre" };
    private static readonly string[] Palette =
        { "#C94A1F", "#5C7A5E", "#A89880", "#4A7CA0", "#8A6A9A", "#B5651D", "#3E7C6A" };

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

    public async Task<IActionResult> OnGetAsync(string token, string? mes = null)
    {
        if (string.IsNullOrWhiteSpace(token)) return NotFound();
        var student = await _db.Users.FirstOrDefaultAsync(u => u.AgendaShareToken == token);
        if (student is null) return NotFound();
        ShareToken = token;

        var nowLocal = DateTime.UtcNow.AddHours(-3);   // ART
        int calY = nowLocal.Year, calM = nowLocal.Month;
        if (!string.IsNullOrEmpty(mes) && DateTime.TryParseExact(mes + "-01", "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var md))
        { calY = md.Year; calM = md.Month; }

        var firstOfMonth = new DateTime(calY, calM, 1, 0, 0, 0, DateTimeKind.Utc);
        CalYear = calY; CalMonth = calM;
        CalDaysInMonth = DateTime.DaysInMonth(calY, calM);
        CalFirstDowOffset = ((int)firstOfMonth.DayOfWeek + 6) % 7;
        CalMonthLabel = $"{MesesEs[calM - 1]} {calY}";
        CalPrevMes = firstOfMonth.AddMonths(-1).ToString("yyyy-MM");
        CalNextMes = firstOfMonth.AddMonths(1).ToString("yyyy-MM");
        CalTodayDay = (calY == nowLocal.Year && calM == nowLocal.Month) ? nowLocal.Day : 0;

        var nextMonth = firstOfMonth.AddMonths(1);
        var events = await _db.DetectedAssignments
            .Where(d => d.StudentId == student.Id && !d.Done
                        && d.DueDate != null && d.DueDate >= firstOfMonth && d.DueDate < nextMonth)
            .OrderBy(d => d.DueDate)
            .ToListAsync();

        // Color por MATERIA (anonimizado: sin nombre del hijo).
        var courses = events.Select(e => e.CourseName).Distinct().ToList();
        var colorByCourse = courses
            .Select((c, i) => (c, Color: Palette[i % Palette.Length]))
            .ToDictionary(x => x.c, x => x.Color);

        foreach (var d in events)
        {
            var day = d.DueDate!.Value.Day;
            if (!CalByDay.TryGetValue(day, out var list)) { list = new(); CalByDay[day] = list; }
            var color = colorByCourse.TryGetValue(d.CourseName, out var c) ? c : "#888";
            list.Add(new Cell(color, LetterFor(d.Type, d.Title), d.Title, d.CourseName));
        }
        CalLegend = courses.Select(c => (c, colorByCourse[c])).ToList();

        // Analítica first-party: registramos la visita (best-effort, salteando bots/previews).
        var ua = Request.Headers.UserAgent.ToString().ToLowerInvariant();
        var isBot = string.IsNullOrEmpty(ua) || ua.Contains("bot") || ua.Contains("crawl")
                    || ua.Contains("spider") || ua.Contains("preview")
                    || ua.Contains("facebookexternalhit") || ua.Contains("whatsapp");
        if (!isBot)
        {
            try
            {
                var referer = Request.Headers.Referer.ToString();
                _db.AgendaViews.Add(new AgendaView
                {
                    StudentId = student.Id,
                    Referrer = string.IsNullOrWhiteSpace(referer) ? null : referer
                });
                await _db.SaveChangesAsync();
            }
            catch { /* la analítica nunca debe romper la página */ }
        }

        return Page();
    }
}
