using System.Text.RegularExpressions;

namespace miTutoria.Web.Inbox;

/// <summary>
/// Parser de los mails de Google Classroom. Se apoya en la ESTRUCTURA (prefijo del
/// subject + layout fijo del template), no en el idioma del contenido.
/// Anatomía documentada en docs/classroom-mail-types.md.
/// </summary>
public static class ClassroomMailParser
{
    // El prefijo del Subject es el discriminador de tipo. Orden importa:
    // "fecha de entrega mañana" antes que cualquier "nueva tarea".
    private static readonly (string prefix, ClassroomItemType type)[] Prefixes =
    {
        ("fecha de entrega mañana", ClassroomItemType.DueReminder),
        ("nueva tarea",             ClassroomItemType.Assignment),
        ("nuevo material",          ClassroomItemType.Material),
        ("nuevo anuncio",           ClassroomItemType.Announcement),
    };

    public static ParsedClassroomItem? Parse(string subject, string from, string to, string plainBody)
    {
        if (string.IsNullOrWhiteSpace(subject)) return null;

        // Sacar "Fwd:" / "RE:" por si se prueba con copias reenviadas.
        var subj = Regex.Replace(subject.Trim(), @"^(fwd|re|rv)\s*:\s*", "", RegexOptions.IgnoreCase).Trim();
        var lower = subj.ToLowerInvariant();

        var item = new ParsedClassroomItem { StudentEmail = ExtractEmail(to) };

        // Tipo + título desde el subject.
        var matched = false;
        foreach (var (prefix, type) in Prefixes)
        {
            if (lower.StartsWith(prefix))
            {
                item.Type = type;
                matched = true;
                break;
            }
        }
        item.Title = ExtractQuoted(subj) ?? StripPrefix(subj);
        if (!matched) item.Type = ClassroomItemType.Unknown;

        item.Teacher = ExtractTeacher(from);

        var body = (plainBody ?? string.Empty).Replace("\r\n", "\n");
        var lines = body.Split('\n').Select(l => l.Trim()).ToList();
        var content = lines.Where(l => !IsNoise(l)).ToList();

        // Materia: la línea siguiente a "Ajustes de notificaciones".
        var anchor = content.FindIndex(l => l.StartsWith("Ajustes de notificaciones", StringComparison.OrdinalIgnoreCase));
        if (anchor >= 0 && anchor + 1 < content.Count)
        {
            item.CourseRaw = content[anchor + 1];
            item.CourseNormalized = NormalizeCourse(item.CourseRaw);
        }

        // Fecha de entrega (solo si aplica).
        var due = Regex.Match(body, @"Fecha de entrega:\s*(.+)", RegexOptions.IgnoreCase);
        if (due.Success) item.DueDateRaw = due.Groups[1].Value.Trim();

        // Ids estables desde las URLs del template.
        var c = Regex.Match(body, @"/c/([A-Za-z0-9_-]+)");
        if (c.Success) item.CourseId = c.Groups[1].Value;
        var a = Regex.Match(body, @"/a/([A-Za-z0-9_-]+)");
        if (a.Success) item.ItemId = a.Groups[1].Value;

        // Descripción (best-effort): la línea siguiente al título.
        var titleIdx = content.FindIndex(l => l.Equals(item.Title, StringComparison.OrdinalIgnoreCase));
        if (titleIdx >= 0 && titleIdx + 1 < content.Count)
        {
            var next = content[titleIdx + 1];
            if (!next.StartsWith("Ver ", StringComparison.OrdinalIgnoreCase) &&
                !next.StartsWith("Fecha de entrega", StringComparison.OrdinalIgnoreCase) &&
                !next.StartsWith("Publicado", StringComparison.OrdinalIgnoreCase))
                item.Description = next;
        }

        return item;
    }

    private static bool IsNoise(string l) =>
        l.Length == 0 ||
        l.StartsWith("<http") || l.StartsWith("http") ||
        l.StartsWith("[image:");

    private static string StripPrefix(string subj)
    {
        var i = subj.IndexOf(':');
        return i >= 0 ? subj[(i + 1)..].Trim().Trim('"', '“', '”') : subj.Trim();
    }

    private static string? ExtractQuoted(string s)
    {
        var m = Regex.Match(s, "[\"“]([^\"”]+)[\"”]");
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static string ExtractEmail(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        var m = Regex.Match(s, @"<([^>]+)>");
        return (m.Success ? m.Groups[1].Value : s).Trim().ToLowerInvariant();
    }

    private static string ExtractTeacher(string from)
    {
        if (string.IsNullOrWhiteSpace(from)) return string.Empty;
        var name = Regex.Replace(from, @"<[^>]*>", "").Trim();          // saca <mail>
        name = Regex.Replace(name, @"\s*\(Classroom\)\s*", "", RegexOptions.IgnoreCase).Trim();
        return name.Trim('"', ' ');
    }

    private static string NormalizeCourse(string raw)
    {
        var s = raw.Trim();
        s = Regex.Replace(s, @"\s+20\d{2}\s*$", "");          // saca el año final
        s = Regex.Replace(s, @"^\d+\S*\s+[A-Za-zÁÉÍÓÚÑ]\s+", ""); // saca "1ro A " (grado + división)
        return s.Trim();
    }
}
