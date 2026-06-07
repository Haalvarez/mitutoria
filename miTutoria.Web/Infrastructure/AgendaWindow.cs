using miTutoria.Web.Inbox;

namespace miTutoria.Web.Infrastructure;

/// <summary>
/// Ventana móvil del calendario: en vez de un mes calendario (que muestra mucho pasado
/// inútil a fin de mes), una ventana de ~10 días atrás y ~20 adelante, alineada a semanas.
/// Compartida por el dashboard y el calendario público para que rindan igual.
/// </summary>
public static class AgendaWindow
{
    public const int DefaultBack = 10;
    public const int DefaultFwd = 20;

    public static readonly string[] MesesAbbr =
        { "ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sep", "oct", "nov", "dic" };

    /// <summary>Letra del tipo: E=Examen (inferido del título), T=Tarea, M=Material, A=Anuncio.</summary>
    public static string LetterFor(ClassroomItemType type, string title)
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

    /// <summary>Una celda de día de la grilla (alineada Lun..Dom).</summary>
    public record Slot(int Index, DateTime Date, int DayNum, string? MonthAbbr, bool IsToday, bool IsPast);

    /// <summary>
    /// Construye la grilla. off desplaza la ventana en días (◀ ▶). Devuelve también el rango
    /// UTC para la query (Kind=Utc, requerido por Npgsql sobre timestamptz) y la etiqueta del rango.
    /// </summary>
    public static (List<Slot> Slots, DateTime FromUtc, DateTime ToUtc, string RangeLabel, int PrevOff, int NextOff)
        Build(DateTime nowUtc, int back, int fwd, int off)
    {
        var today = (nowUtc + TimeSpan.FromHours(-3)).Date;   // hoy en ART
        var windowStart = today.AddDays(off - back);
        var windowEnd = today.AddDays(off + fwd);

        // Alinear: arranca el lunes <= windowStart, termina el domingo >= windowEnd.
        var gridStart = windowStart.AddDays(-(((int)windowStart.DayOfWeek + 6) % 7));
        var gridEnd = windowEnd.AddDays(6 - (((int)windowEnd.DayOfWeek + 6) % 7));

        var slots = new List<Slot>();
        var total = (gridEnd - gridStart).Days + 1;
        for (var i = 0; i < total; i++)
        {
            var date = gridStart.AddDays(i);
            slots.Add(new Slot(i, date, date.Day,
                date.Day == 1 ? MesesAbbr[date.Month - 1] : null,
                date == today, date < today));
        }

        var fromUtc = DateTime.SpecifyKind(gridStart, DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(gridEnd.AddDays(1), DateTimeKind.Utc);   // exclusivo
        var rangeLabel = $"{windowStart.Day} {MesesAbbr[windowStart.Month - 1]} – {windowEnd.Day} {MesesAbbr[windowEnd.Month - 1]}";
        return (slots, fromUtc, toUtc, rangeLabel, off - (back + fwd), off + (back + fwd));
    }
}
