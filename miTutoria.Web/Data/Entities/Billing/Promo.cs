namespace miTutoria.Web.Data.Entities.Billing;

/// <summary>
/// Cupón de descuento para cobrar menos (familia y amigos). Se carga desde el admin.
/// La familia escribe la Clave al pagar → la cuota pasa a ser AmountArs en vez de CUOTA_ARS.
/// Vigencia opcional (Desde/Hasta) y tope de usos opcional (MaxUses).
/// </summary>
public class Promo
{
    public int Id { get; set; }
    /// <summary>Clave que escribe la familia (normalizada a MAYÚSCULAS sin espacios). Única.</summary>
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>Precio que paga la familia con esta promo (ARS).</summary>
    public decimal AmountArs { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public bool Active { get; set; } = true;
    /// <summary>Tope de usos (pagos aprobados). null = sin tope.</summary>
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Normaliza una clave escrita por el usuario para comparar/guardar.</summary>
    public static string Normalize(string? raw) =>
        (raw ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", "");

    public bool IsUsable(DateTime nowUtc) =>
        Active
        && (!ValidFrom.HasValue || ValidFrom.Value <= nowUtc)
        && (!ValidUntil.HasValue || ValidUntil.Value >= nowUtc)
        && (!MaxUses.HasValue || UsedCount < MaxUses.Value);
}
