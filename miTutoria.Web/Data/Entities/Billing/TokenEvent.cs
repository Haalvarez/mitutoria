namespace miTutoria.Web.Data.Entities.Billing;

public class TokenEvent
{
    public int Id { get; set; }
    public int FamilyId { get; set; }
    public int? UserId { get; set; }
    public int? SessionId { get; set; }
    public int TokensIn { get; set; }
    public int TokensOut { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
    public string Feature { get; set; } = string.Empty;
    public decimal CostUsd { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
