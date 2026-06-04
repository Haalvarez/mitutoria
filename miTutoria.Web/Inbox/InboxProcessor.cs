using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;
using miTutoria.Web.Data.Entities.Academic;
using miTutoria.Web.Data.Entities.Inbox;

namespace miTutoria.Web.Inbox;

/// <summary>
/// Capa 2b: toma los mails crudos sin procesar, los parsea, mapea al alumno por
/// ClassroomEmail, crea/fusiona la materia (Classroom) y hace upsert del DetectedAssignment.
/// Gateado por el kill-switch global (INBOX_FEATURE_ENABLED) y por familia (Family.InboxEnabled).
/// La RECEPCIÓN no se gatea (eso pasa en el webhook); esto es el procesamiento.
/// </summary>
public class InboxProcessor
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;

    public InboxProcessor(AppDbContext db, IConfiguration cfg)
    {
        _db = db;
        _cfg = cfg;
    }

    public async Task<int> ProcessPendingAsync(CancellationToken ct = default)
    {
        // Kill-switch global: si está apagado, los crudos esperan sin procesarse.
        if (!_cfg.GetValue("INBOX_FEATURE_ENABLED", false)) return 0;

        var pending = await _db.InboxMessagesRaw
            .Where(m => !m.Processed)
            .OrderBy(m => m.MessageDate)
            .Take(200)
            .ToListAsync(ct);

        var processed = 0;

        foreach (var raw in pending)
        {
            var parsed = ClassroomMailParser.Parse(raw.Subject, raw.FromAddress, raw.ToAddress, raw.PlainBody);
            if (parsed is null) { raw.Processed = true; continue; }

            // Mapear al alumno por su casilla del colegio.
            var email = parsed.StudentEmail.ToLowerInvariant();
            var student = await _db.Users.FirstOrDefaultAsync(
                u => u.ClassroomEmail != null && u.ClassroomEmail.ToLower() == email, ct);
            if (student is null) continue; // sin alumno configurado → dejar pendiente, reintenta luego

            // Gate por familia.
            var family = await _db.Families.FindAsync(new object?[] { student.FamilyId }, ct);
            if (family is null || !family.InboxEnabled) continue; // no habilitada → esperar

            // Resolver/crear la materia (Classroom) por nombre normalizado.
            int? classroomId = null;
            var courseName = parsed.CourseNormalized.Trim();
            if (courseName.Length > 0)
            {
                var lc = courseName.ToLowerInvariant();
                var existing = await _db.Classrooms.FirstOrDefaultAsync(
                    c => c.StudentId == student.Id && c.Name.ToLower() == lc, ct);
                if (existing is null)
                {
                    existing = new Classroom
                    {
                        StudentId = student.Id,
                        SubjectId = null,
                        Name = courseName.Length > 40 ? courseName[..40] : courseName,
                        Mode = InferMode(courseName),
                        SystemPrompt = string.Empty,
                        LastActiveAt = DateTime.UtcNow
                    };
                    _db.Classrooms.Add(existing);
                    await _db.SaveChangesAsync(ct); // necesitamos el Id
                }
                classroomId = existing.Id;
            }

            // Upsert del assignment (dedup por student + itemId si lo hay).
            DetectedAssignment? da = null;
            if (!string.IsNullOrEmpty(parsed.ItemId))
                da = await _db.DetectedAssignments.FirstOrDefaultAsync(
                    d => d.StudentId == student.Id && d.ItemId == parsed.ItemId, ct);

            if (da is null)
            {
                da = new DetectedAssignment { StudentId = student.Id, ItemId = parsed.ItemId };
                _db.DetectedAssignments.Add(da);
            }

            da.ClassroomId  = classroomId;
            da.Type         = parsed.Type;
            da.Title        = parsed.Title;
            da.CourseName   = parsed.CourseNormalized;
            da.Teacher      = parsed.Teacher;
            da.Description   = parsed.Description;
            da.DueDateRaw   = parsed.DueDateRaw;
            da.DueDate      = ParseSpanishDate(parsed.DueDateRaw, raw.MessageDate);
            da.CourseId     = parsed.CourseId;
            da.MessageDate  = raw.MessageDate;

            raw.Processed = true;
            processed++;
        }

        await _db.SaveChangesAsync(ct);
        return processed;
    }

    // Mismo criterio que el Aula: materias procedimentales → Resolución; resto → Comprensión.
    private static PedagogicalMode InferMode(string name)
    {
        var n = name.ToLowerInvariant();
        string[] resolucion =
        {
            "matem", "mate", "álgebra", "algebra", "geometr", "trigonometr",
            "física", "fisica", "químic", "quimic", "cálculo", "calculo",
            "contab", "estadística", "estadistica", "aritmét", "aritmet"
        };
        return resolucion.Any(k => n.Contains(k)) ? PedagogicalMode.Resolucion : PedagogicalMode.Comprension;
    }

    private static readonly Dictionary<string, int> Months = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ene"] = 1, ["feb"] = 2, ["mar"] = 3, ["abr"] = 4, ["may"] = 5, ["jun"] = 6,
        ["jul"] = 7, ["ago"] = 8, ["sep"] = 9, ["set"] = 9, ["oct"] = 10, ["nov"] = 11, ["dic"] = 12
    };

    // "28 may" / "5 de junio" → DateTime (año del mail). Best-effort; null si no se puede.
    public static DateTime? ParseSpanishDate(string? raw, DateTime reference)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var m = Regex.Match(raw, @"(\d{1,2})\s*(?:de\s+)?([A-Za-zÁÉÍÓÚáéíóú]+)");
        if (!m.Success || !int.TryParse(m.Groups[1].Value, out var day)) return null;

        var key = m.Groups[2].Value.TrimEnd('.').ToLowerInvariant();
        var prefix = key.Length >= 3 ? key[..3] : key;
        if (!Months.TryGetValue(prefix, out var month) || day < 1 || day > 31) return null;

        try { return new DateTime(reference.Year, month, day, 0, 0, 0, DateTimeKind.Utc); }
        catch { return null; }
    }
}
