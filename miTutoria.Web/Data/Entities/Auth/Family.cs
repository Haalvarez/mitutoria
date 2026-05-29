namespace miTutoria.Web.Data.Entities.Auth;

public enum ParentRole { Padre, Madre }

public class Family
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public ParentRole ParentRole { get; set; }
    public string? MagicToken { get; set; }
    public DateTime? MagicTokenExpiry { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}
