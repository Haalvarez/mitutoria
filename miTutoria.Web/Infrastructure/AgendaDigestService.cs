using System.Text;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;
using miTutoria.Web.Inbox;

namespace miTutoria.Web.Infrastructure;

/// <summary>
/// Digest a TU Telegram (el chat del admin): cada ~3h junta los eventos NUEVOS de Classroom
/// (no avisados) de las familias con la Agenda activa, agrupados por hijo, y manda un solo mensaje.
/// Solo notifica en positivo (si no hay nada nuevo, no manda). Marca lo avisado para no repetir.
/// En el primer arranque NO vuelca el backlog: solo lo detectado en las últimas 24h.
/// </summary>
public class AgendaDigestService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TelegramService _telegram;
    private readonly IConfiguration _config;
    private readonly ILogger<AgendaDigestService> _logger;

    public AgendaDigestService(IServiceScopeFactory scopeFactory, TelegramService telegram,
        IConfiguration config, ILogger<AgendaDigestService> logger)
    {
        _scopeFactory = scopeFactory;
        _telegram = telegram;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); }
        catch (TaskCanceledException) { return; }

        var hours = Math.Max(1, _config.GetValue("DIGEST_HOURS", 3));
        var interval = TimeSpan.FromHours(hours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogWarning(ex, "AgendaDigest: fallo en la pasada"); }

            try { await Task.Delay(interval, stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        if (!_config.GetValue("INBOX_FEATURE_ENABLED", false)) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;
        var freshCutoff = now.AddHours(-24);   // anti-dump: solo se manda lo reciente

        var famIds = await db.Families.Where(f => f.InboxEnabled).Select(f => f.Id).ToListAsync(ct);
        if (famIds.Count == 0) return;

        var students = await db.Users
            .Where(u => u.Role == Data.Entities.Auth.UserRole.Student && famIds.Contains(u.FamilyId))
            .Select(u => new { u.Id, Name = u.Nickname ?? u.FullName })
            .ToListAsync(ct);
        var nameById = students.ToDictionary(s => s.Id, s => s.Name);
        var studentIds = students.Select(s => s.Id).ToList();
        if (studentIds.Count == 0) return;

        var candidates = await db.DetectedAssignments
            .Where(d => d.NotifiedAt == null && studentIds.Contains(d.StudentId))
            .ToListAsync(ct);
        if (candidates.Count == 0) return;

        // Lo que efectivamente se avisa: solo lo detectado en las últimas 24h.
        var toSend = candidates.Where(d => d.DetectedAt >= freshCutoff).ToList();

        if (toSend.Count > 0)
        {
            var sb = new StringBuilder();
            sb.Append("📅 <b>Novedades de Classroom</b>\n");
            foreach (var grp in toSend.GroupBy(d => d.StudentId)
                                      .OrderBy(g => nameById.GetValueOrDefault(g.Key, "")))
            {
                sb.Append($"\n<b>{Escape(nameById.GetValueOrDefault(grp.Key, "—"))}</b>\n");
                foreach (var d in grp.OrderBy(d => d.DueDate ?? DateTime.MaxValue))
                {
                    var emoji = d.Type switch
                    {
                        ClassroomItemType.DueReminder => "⏰",
                        ClassroomItemType.Assignment => "📝",
                        ClassroomItemType.Material => "📎",
                        ClassroomItemType.Announcement => "📣",
                        _ => "•"
                    };
                    var due = string.IsNullOrEmpty(d.DueDateRaw) ? "" : $" (entrega {Escape(d.DueDateRaw)})";
                    sb.Append($"• {emoji} {Escape(d.Title)} — {Escape(d.CourseName)}{due}\n");
                }
            }
            await _telegram.SendAsync(sb.ToString());
            _logger.LogInformation("AgendaDigest: avisó {Count} novedad(es)", toSend.Count);
        }

        // Marcamos TODO lo candidato como avisado (incluye el backlog viejo → no se vuelca nunca).
        foreach (var d in candidates) d.NotifiedAt = now;
        await db.SaveChangesAsync(ct);
    }

    private static string Escape(string? s) =>
        (s ?? string.Empty).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
