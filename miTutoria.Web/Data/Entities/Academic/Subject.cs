namespace miTutoria.Web.Data.Entities.Academic;

public class Subject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Classroom> Classrooms { get; set; } = new List<Classroom>();
}
