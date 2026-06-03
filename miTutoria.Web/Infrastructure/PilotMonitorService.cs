using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;

namespace miTutoria.Web.Infrastructure;

/// <summary>
/// Recorre las familias del piloto cada 6h y avisa a Telegram SOLO en positivo:
///  - Costo: el ciclo actual de la familia superó el umbral en USD.
///  - Renovación: el trial/pago vence en ≤ 3 días (o ya venció).
/// Dedup por ciclo: no repite el aviso hasta que cambia el ancla del ciclo (PaidUntil/TrialEndsAt).
/// El "ciclo" se ancla en la fecha de la familia (no el 1° de mes): ciclo = [ancla - 1 mes, ancla).
/// </summary>
public class PilotMonitorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TelegramService _telegram;
    private readonly IConfiguration _config;
    private readonly ILogger<PilotMonitorService> _logger;

    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    public PilotMonitorService(
        IServiceScopeFactory scopeFactory,
        TelegramService telegram,
        IConfiguration config,
        ILogger<PilotMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _telegram = telegram;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // No chocar con el arranque ni con las migraciones.
        try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); }
        catch (TaskCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunChecksAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogWarning(ex, "PilotMonitor: fallo en el chequeo periódico"); }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }

    private async Task RunChecksAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var alertUsd = _config.GetValue<decimal>("TERMICA_USD_ALERTA", 5m);
        var now = DateTime.UtcNow;

        var families = await db.Families
            .Where(f => f.SubscriptionStatus == "trial" || f.SubscriptionStatus == "active")
            .ToListAsync(ct);

        foreach (var f in families)
        {
            var anchor = f.PaidUntil ?? f.TrialEndsAt;
            var marker = anchor?.ToString("yyyy-MM-dd") ?? "none";
            var cycleStart = anchor?.AddMonths(-1) ?? now.AddDays(-30);
            var changed = false;

            // ── Costo del ciclo ──
            var cycleCost = await db.TokenEvents
                .Where(t => t.FamilyId == f.Id && t.CreatedAt >= cycleStart)
                .SumAsync(t => t.CostUsd, ct);

            if (cycleCost >= alertUsd && f.CostAlertMarker != marker)
            {
                await _telegram.SendAsync(
                    $"💸 <b>{f.Nickname ?? f.Name}</b> ({f.Email}) lleva <b>${cycleCost:F2}</b> en el ciclo actual " +
                    $"(alerta ≥ ${alertUsd:F0}). Mirá el detalle por hijo en /admin.");
                f.CostAlertMarker = marker;
                changed = true;
            }

            // ── Renovación / vencimiento (≤ 3 días o vencido) ──
            if (anchor.HasValue && anchor.Value <= now.AddDays(3) && f.RenewalAlertMarker != marker)
            {
                var tipo = f.PaidUntil.HasValue ? "pago" : "trial";
                var venceTxt = anchor.Value <= now ? "ya venció" : $"vence el {anchor.Value:dd/MM}";
                await _telegram.SendAsync(
                    $"⏰ <b>{f.Nickname ?? f.Name}</b> ({f.Email}): el {tipo} {venceTxt}.");
                f.RenewalAlertMarker = marker;
                changed = true;
            }

            if (changed) await db.SaveChangesAsync(ct);
        }
    }
}
