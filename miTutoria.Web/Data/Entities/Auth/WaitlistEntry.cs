namespace miTutoria.Web.Data.Entities.Auth;

public class WaitlistEntry
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
