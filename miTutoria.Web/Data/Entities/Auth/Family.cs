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

    // Login del padre con contraseña (hasheada con PasswordHasher). null = todavía no la creó.
    // El magic link sirve solo para crearla (onboarding) o resetearla.
    public string? PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string SubscriptionStatus { get; set; } = "waitlist";
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? PaidUntil { get; set; }
    public DateTime? ConsentAt { get; set; }
    public string? ConsentIp { get; set; }
    public string? ConsentVersion { get; set; }

    // Dedup de alertas del scheduler: guardan el marcador del ciclo ya avisado (yyyy-MM-dd del ancla)
    public string? CostAlertMarker { get; set; }
    public string? RenewalAlertMarker { get; set; }

    // Track 2: rollout de la agenda de Classroom, familia por familia (gatea UI + procesamiento)
    public bool InboxEnabled { get; set; }

    // Cobro: rollout del botón "Quiero pagar" familia por familia (para probar sin habilitarlo a todos)
    public bool PayEnabled { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();

    public bool IsAccessAllowed =>
        SubscriptionStatus is "trial" or "active";
}
