using miTutoria.Web.Inbox;

namespace miTutoria.Web.Data.Entities.Inbox;

/// <summary>
/// Tarea / material / anuncio detectado por el parser a partir de un mail de Classroom.
/// Estructurado, retención indefinida. Capa 2b lo crea; la Agenda (Capa 3/4) lo muestra.
/// Dedup por (StudentId, ItemId) cuando hay ItemId; los anuncios sin ItemId se insertan sueltos.
/// </summary>
public class DetectedAssignment
{
    public int Id { get; set; }

    /// <summary>Alumno (User) dueño — resuelto por ClassroomEmail.</summary>
    public int StudentId { get; set; }

    /// <summary>Materia de la mochila = Classroom del alumno — auto-creada/fusionada (null si sin resolver).</summary>
    public int? ClassroomId { get; set; }

    public ClassroomItemType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;   // normalizado
    public string Teacher { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Texto crudo de la fecha, ej. "28 may".</summary>
    public string? DueDateRaw { get; set; }

    /// <summary>Fecha de entrega parseada (best-effort, año del mail).</summary>
    public DateTime? DueDate { get; set; }

    public string? CourseId { get; set; }
    public string? ItemId { get; set; }

    public DateTime MessageDate { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Cuándo se incluyó en un digest de Telegram (null = todavía no avisado).</summary>
    public DateTime? NotifiedAt { get; set; }

    /// <summary>El alumno la marcó como hecha (el ✓ que da el micro-logro).</summary>
    public bool Done { get; set; }
    public DateTime? DoneAt { get; set; }
}
