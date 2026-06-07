namespace miTutoria.Web.Data.Entities.Academic;

/// <summary>
/// Analítica first-party del calendario público compartido: una fila por visita.
/// Sin cookies, sin GA. Sirve para mostrarle al padre cuántas veces vieron su agenda.
/// </summary>
public class AgendaView
{
    public int Id { get; set; }
    public int StudentId { get; set; }   // alumno dueño del token compartido
    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
    public string? Referrer { get; set; }
    /// <summary>"view" = abrió el calendario | "cta" = clickeó "Conocé miTutorIA" → landing.</summary>
    public string Kind { get; set; } = "view";
}
