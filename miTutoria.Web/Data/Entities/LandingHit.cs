namespace miTutoria.Web.Data.Entities;

/// <summary>
/// Analítica first-party de la landing: una fila por visita humana a la home.
/// Sin cookies, sin GA. Sirve para medir el alcance de un envío (WhatsApp, etc.):
/// cuántos entraron vs cuántos se anotaron en la waitlist.
/// Los bots/crawlers (incluido el preview de WhatsApp) NO se registran.
/// </summary>
public class LandingHit
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Path { get; set; }
    public string? Referrer { get; set; }
    public string? UserAgent { get; set; }
    public bool IsMobile { get; set; }
}
