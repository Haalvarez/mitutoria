namespace miTutoria.Web.Data.Entities.Inbox;

/// <summary>
/// Mail crudo de Google Classroom recibido vía el Apps Script de la cuenta del hijo.
/// Capa 1 del Track 2: se guarda tal cual entra; el parser (Capa 2) lo procesa después.
/// La recepción NUNCA se gatea por flag — los datos entran siempre.
/// </summary>
public class InboxMessageRaw
{
    public int Id { get; set; }

    /// <summary>Origen del POST, ej. "classroom-apps-script".</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Id del mensaje en Gmail — clave de dedup.</summary>
    public string GmailId { get; set; } = string.Empty;

    /// <summary>Fecha original del mail (del header reenviado/original).</summary>
    public DateTime MessageDate { get; set; }

    /// <summary>Casilla destino original — identifica al hijo (ej. dariaalvarezblardoni@...).</summary>
    public string ToAddress { get; set; } = string.Empty;

    /// <summary>Remitente — docente + "(Classroom)".</summary>
    public string FromAddress { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    /// <summary>Cuerpo en texto plano — lo que parsea la Capa 2.</summary>
    public string PlainBody { get; set; } = string.Empty;

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Lo marca el parser (Capa 2) cuando ya extrajo la tarea/material.</summary>
    public bool Processed { get; set; }
}
