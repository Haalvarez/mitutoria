namespace miTutoria.Web.Data.Entities.Auth;

public class Family
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? MagicToken { get; set; }
    public DateTime? MagicTokenExpiry { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}
