using miTutoria.Web.Data.Entities.Academic;

namespace miTutoria.Web.Data.Entities.Auth;

public enum UserRole { Parent, Student }

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public int FamilyId { get; set; }
    public Family Family { get; set; } = null!;
    public int? Grade { get; set; }
    public DateTime? BirthDate { get; set; }
    public ICollection<Classroom> Classrooms { get; set; } = new List<Classroom>();
}