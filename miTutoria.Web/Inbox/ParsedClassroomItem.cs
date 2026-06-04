namespace miTutoria.Web.Inbox;

public enum ClassroomItemType
{
    Unknown,
    Assignment,    // "Nueva tarea"
    DueReminder,   // "Fecha de entrega mañana"
    Material,      // "Nuevo material"
    Announcement   // "Nuevo anuncio"
}

/// <summary>
/// Resultado de parsear un mail de Google Classroom (formato original que lee el Apps Script).
/// Lógica pura, sin DB. La resolución hijo/materia y el guardado viven en la Capa 2b.
/// </summary>
public class ParsedClassroomItem
{
    public ClassroomItemType Type { get; set; }
    public string Title { get; set; } = string.Empty;

    /// <summary>Casilla destino original — identifica al hijo (del "to").</summary>
    public string StudentEmail { get; set; } = string.Empty;

    /// <summary>Nombre del curso tal cual, ej. "1ro A Prácticas del Lenguaje 2026".</summary>
    public string CourseRaw { get; set; } = string.Empty;

    /// <summary>Nombre limpio para la mochila, ej. "Prácticas del Lenguaje".</summary>
    public string CourseNormalized { get; set; } = string.Empty;

    public string Teacher { get; set; } = string.Empty;

    /// <summary>Texto crudo de la fecha de entrega, ej. "28 may" (null si no aplica).</summary>
    public string? DueDateRaw { get; set; }

    public string? Description { get; set; }

    /// <summary>Id estable del curso (de la URL /c/&lt;id&gt;).</summary>
    public string? CourseId { get; set; }

    /// <summary>Id del item (de la URL /a/&lt;id&gt;) — clave de dedup del assignment.</summary>
    public string? ItemId { get; set; }
}
