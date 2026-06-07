using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;
using miTutoria.Web.Data.Entities.Academic;
using miTutoria.Web.Inbox;
using miTutoria.Web.Infrastructure;

namespace miTutoria.Web.Pages.Agenda;

/// <summary>
/// Calendario PÚBLICO y anónimo de una agenda compartida (token por alumno).
/// Ventana móvil (10 atrás / 20 adelante), anonimizado: materia + tipo + fecha, SIN nombre del hijo.
/// Marketing encubierto: branding + CTA a mitutoria.app. Registra cada visita (analítica first-party).
/// </summary>
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public string ShareToken { get; private set; } = string.Empty;
    public string BaseUrl { get; private set; } = string.Empty;   // para OG tags (preview de WhatsApp)
    public string GradeLabel { get; private set; } = string.Empty; // contexto anónimo, ej. "1° Secundaria"
    public DateTime? LastUpdate { get; private set; }              // último DetectedAt (cuándo se capturó data por última vez)
    public string CalRangeLabel { get; private set; } = string.Empty;
    public int CalPrevOff { get; private set; }
    public int CalNextOff { get; private set; }
    public List<CalDayVM> CalDays { get; private set; } = new();
    public List<(string Name, string Color)> CalLegend { get; private set; } = new();
    public List<Cell> UndatedUpcoming { get; private set; } = new();   // entregas sin fecha parseada
    public bool HasCalendar => CalDays.Any(d => d.Events.Count > 0);
    public record Cell(string Color, string Letter, string Title, string CourseName);
    public record CalDayVM(int Index, int DayNum, string? MonthAbbr, bool IsToday, bool IsPast, List<Cell> Events);

    private static readonly string[] Palette =
        { "#C94A1F", "#5C7A5E", "#A89880", "#4A7CA0", "#8A6A9A", "#B5651D", "#3E7C6A" };

    public async Task<IActionResult> OnGetAsync(string token, int off = 0)
    {
        if (string.IsNullOrWhiteSpace(token)) return NotFound();
        var student = await _db.Users.FirstOrDefaultAsync(u => u.AgendaShareToken == token);
        if (student is null) return NotFound();
        ShareToken = token;
        BaseUrl = $"{Request.Scheme}://{Request.Host}";

        // Contexto anónimo (NO el nombre del hijo): "1ro A" de Classroom + nivel; si no, el perfil.
        var nivel = student.SchoolLevel == Data.Entities.Auth.SchoolLevel.Secundario ? "Secundaria" : "Primaria";
        GradeLabel = !string.IsNullOrEmpty(student.GradeSection)
            ? $"{student.GradeSection} {nivel}"
            : (student.Grade is int g ? $"{g}° {nivel}" : nivel);

        // Última captura de datos (el Apps Script de Classroom corre ~cada hora).
        LastUpdate = await _db.DetectedAssignments
            .Where(d => d.StudentId == student.Id)
            .MaxAsync(d => (DateTime?)d.DetectedAt);

        var (slots, fromUtc, toUtc, rangeLabel, prevOff, nextOff) =
            AgendaWindow.Build(DateTime.UtcNow, AgendaWindow.DefaultBack, AgendaWindow.DefaultFwd, off);
        CalRangeLabel = rangeLabel; CalPrevOff = prevOff; CalNextOff = nextOff;

        var events = await _db.DetectedAssignments
            .Where(d => d.StudentId == student.Id && !d.Done
                        && d.DueDate != null && d.DueDate >= fromUtc && d.DueDate < toUtc)
            .ToListAsync();

        // Entregas SIN fecha parseada (tarea/recordatorio): no caen en un día pero igual importan.
        var undated = await _db.DetectedAssignments
            .Where(d => d.StudentId == student.Id && !d.Done && d.DueDate == null
                        && (d.Type == ClassroomItemType.Assignment || d.Type == ClassroomItemType.DueReminder))
            .OrderByDescending(d => d.MessageDate)
            .Take(20)
            .ToListAsync();

        // Color por MATERIA (anonimizado), de TODAS las materias (con y sin fecha).
        var courses = events.Select(e => e.CourseName)
            .Concat(undated.Select(d => d.CourseName))
            .Distinct().ToList();
        var colorByCourse = courses
            .Select((c, i) => (c, Color: Palette[i % Palette.Length]))
            .ToDictionary(x => x.c, x => x.Color);
        string ColorOf(string course) => colorByCourse.TryGetValue(course, out var c) ? c : "#888";

        var byDate = new Dictionary<DateTime, List<Cell>>();
        foreach (var d in events)
        {
            var date = d.DueDate!.Value.Date;
            if (!byDate.TryGetValue(date, out var list)) { list = new(); byDate[date] = list; }
            list.Add(new Cell(ColorOf(d.CourseName), AgendaWindow.LetterFor(d.Type, d.Title), d.Title, d.CourseName));
        }
        CalDays = slots.Select(sl => new CalDayVM(sl.Index, sl.DayNum, sl.MonthAbbr, sl.IsToday, sl.IsPast,
            byDate.GetValueOrDefault(sl.Date.Date) ?? new())).ToList();
        UndatedUpcoming = undated
            .Select(d => new Cell(ColorOf(d.CourseName), AgendaWindow.LetterFor(d.Type, d.Title), d.Title, d.CourseName))
            .ToList();
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
