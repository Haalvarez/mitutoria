using miTutoria.Web.Data.Entities.Auth;

namespace miTutoria.Web.Data.Entities.Academic;

// Modo pedagógico de la materia: define qué "trabajo del alumno" no hace el tutor.
// Resolucion (mate/física): no da el resultado, guía el procedimiento.
// Comprension (historia/lengua): explica conceptos, pero el alumno sintetiza (no escribe resúmenes ni llena cuadros).
public enum PedagogicalMode
{
    Resolucion = 0,
    Comprension = 1
}

public class Classroom
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
    public int? SubjectId { get; set; }
    public Subject? Subject { get; set; }
    // La materia (cuaderno) — un alumno puede tener varias. Mismo pupitre, distinto cuaderno.
    public string Name { get; set; } = "General";
    public PedagogicalMode Mode { get; set; } = PedagogicalMode.Comprension;
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
    public string SystemPrompt { get; set; } = string.Empty;
    public string? Material { get; set; }
    public string? CompactSummary { get; set; }
    // Secciones temáticas del PDF (JSON: [{title, content}])
    public string? MaterialSections { get; set; }
    public int MaterialSectionIndex { get; set; }
    public string? MaterialOcrSource { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
