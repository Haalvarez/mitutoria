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
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
