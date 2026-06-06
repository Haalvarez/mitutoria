namespace miTutoria.Web.Data.Entities.Billing;

/// <summary>
/// Un intento/registro de pago de la cuota mensual de una familia (MercadoPago Checkout Pro).
/// Se crea al generar la preference (status "pending") y se actualiza desde el webhook.
/// El MpPaymentId da idempotencia: el webhook puede llegar varias veces.
/// </summary>
public class Payment
{
    public int Id { get; set; }
    public int FamilyId { get; set; }

    /// <summary>Id de la "preference" de Checkout Pro (lo que abre el link/QR).</summary>
    public string? PreferenceId { get; set; }

    /// <summary>Id del pago real en MercadoPago (llega por el webhook). Único cuando existe.</summary>
    public string? MpPaymentId { get; set; }

    /// <summary>Ancla del ciclo que paga (yyyy-MM-dd), para auditar a qué período corresponde.</summary>
    public string? CycleMarker { get; set; }

    public decimal AmountArs { get; set; }

    /// <summary>pending | approved | rejected | cancelled — espejo de MercadoPago.</summary>
    public string Status { get; set; } = "pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
}
