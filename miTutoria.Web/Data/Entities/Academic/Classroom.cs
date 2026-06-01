using miTutoria.Web.Data.Entities.Auth;

namespace miTutoria.Web.Data.Entities.Academic;

public class Classroom
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
    public int? SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public string? Material { get; set; }
    public string? CompactSummary { get; set; }
    // Secciones temáticas del PDF (JSON: [{title, content}])
    public string? MaterialSections { get; set; }
    public int MaterialSectionIndex { get; set; }
    public string? MaterialOcrSource { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
