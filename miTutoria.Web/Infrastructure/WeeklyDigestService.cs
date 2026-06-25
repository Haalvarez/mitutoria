using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;
using miTutoria.Web.Data.Entities.Auth;
using Resend;

namespace miTutoria.Web.Infrastructure;

/// <summary>
/// Resumen semanal por mail a los padres que lo activaron: los viernes a las 20hs ARG,
/// un solo correo por familia con un párrafo cálido por cada hijo (qué trabajó, cómo le fue;
/// si no estudió esta semana, lo avisa con suavidad). Lo redacta Claude desde la actividad
/// de la semana + los resúmenes acumulados por materia.
/// Dedup con Family.LastDigestSentAt para no repetir el envío el mismo viernes (aunque el
/// servicio chequee varias veces o la app reinicie).
/// </summary>
public class WeeklyDigestService : BackgroundService
{
    private const string ClaudeModel = "claude-haiku-4-5-20251001";
    private static readonly TimeSpan CheckEvery = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ArgOffset = TimeSpan.FromHours(-3); // UTC-3 fijo, sin DST

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<WeeklyDigestService> _logger;

    public WeeklyDigestService(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<WeeklyDigestService> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
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
            try { await TickAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogWarning(ex, "WeeklyDigest: fallo en el tick"); }

            try { await Task.Delay(CheckEvery, stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        if (!_config.GetValue("WEEKLY_DIGEST_ENABLED", true)) return;

        var sendHour = _config.GetValue("WEEKLY_DIGEST_HOUR", 20); // hora ARG
        var nowUtc = DateTime.UtcNow;
        var nowArg = nowUtc + ArgOffset;

        // Solo los viernes, de la hora objetivo en adelante.
        if (nowArg.DayOfWeek != DayOfWeek.Friday || nowArg.Hour < sendHour) return;

        // Cutoff = hoy (viernes) a las sendHour ARG, en UTC. Enviamos si no se mandó desde ese instante.
        var cutoffArg = nowArg.Date.AddHours(sendHour);
        var cutoffUtc = DateTime.SpecifyKind(cutoffArg - ArgOffset, DateTimeKind.Utc);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var resend = scope.ServiceProvider.GetRequiredService<IResend>();

        var families = await db.Families
            .Where(f => f.WeeklyDigestOptIn
                        && (f.SubscriptionStatus == "trial" || f.SubscriptionStatus == "active")
                        && (f.LastDigestSentAt == null || f.LastDigestSentAt < cutoffUtc))
            .Include(f => f.Users.Where(u => u.Role == UserRole.Student))
            .ToListAsync(ct);

        foreach (var family in families)
        {
            try
            {
                var sent = await SendDigestAsync(db, resend, family, ct);
                // Marcamos el envío aunque no haya habido nada que contar, para no reintentar todo el viernes.
                family.LastDigestSentAt = nowUtc;
                await db.SaveChangesAsync(ct);
                if (sent) _logger.LogInformation("WeeklyDigest: enviado a la familia {FamilyId}", family.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WeeklyDigest: falló el envío a la familia {FamilyId}", family.Id);
            }
        }
    }

    private async Task<bool> SendDigestAsync(AppDbContext db, IResend resend, Family family, CancellationToken ct)
    {
        var studentIds = family.Users.Select(u => u.Id).ToList();
        if (studentIds.Count == 0) return false;

        var weekStartUtc = DateTime.UtcNow.AddDays(-7);

        // Actividad de chat de la semana, por hijo.
        var weekChat = await db.TokenEvents
            .Where(t => t.FamilyId == family.Id && t.Feature == "chat"
                        && t.UserId.HasValue && t.CreatedAt >= weekStartUtc)
            .Select(t => new { t.UserId, t.CreatedAt })
            .ToListAsync(ct);

        // Resúmenes acumulados de cada materia (lo que viene trabajando cada hijo).
        var classrooms = await db.Classrooms
            .Where(c => studentIds.Contains(c.StudentId))
            .Select(c => new { c.StudentId, c.Name, c.CompactSummary })
            .ToListAsync(ct);

        // Input para Claude: una sección por hijo.
        var sb = new StringBuilder();
        foreach (var child in family.Users)
        {
            var name = child.Nickname ?? child.FullName;
            var myWeek = weekChat.Where(e => e.UserId == child.Id).ToList();
            var days = myWeek.Select(e => (e.CreatedAt + ArgOffset).Date).Distinct().Count();
            var exchanges = myWeek.Count;

            sb.AppendLine($"### {name}");
            if (exchanges == 0)
            {
                sb.AppendLine("Esta semana no usó la plataforma.");
            }
            else
            {
                sb.AppendLine($"Días que estudió esta semana: {days}. Intercambios con el tutor: {exchanges}.");
                var mySummaries = classrooms
                    .Where(c => c.StudentId == child.Id && !string.IsNullOrWhiteSpace(c.CompactSummary))
                    .ToList();
                if (mySummaries.Count > 0)
                {
                    sb.AppendLine("Lo que viene trabajando, por materia:");
                    foreach (var c in mySummaries)
                        sb.AppendLine($"- {(string.IsNullOrWhiteSpace(c.Name) ? "General" : c.Name)}: {c.CompactSummary}");
                }
            }
            sb.AppendLine();
        }

        var parentName = family.Nickname ?? family.Name;
        var bodyText = await GenerateAsync(sb.ToString(), ct);
        if (string.IsNullOrWhiteSpace(bodyText)) return false;

        var message = new EmailMessage
        {
            From = _config["RESEND_FROM"] ?? "noreply@mitutoria.app",
            Subject = "El resumen de la semana de tus hijos — miTutorIA",
            HtmlBody = BuildEmailHtml(bodyText)
        };
        message.To.Add(family.Email);
        await resend.EmailSendAsync(message);
        return true;
    }

    private async Task<string> GenerateAsync(string childrenData, CancellationToken ct)
    {
        var system =
            "Sos el tutor digital de miTutorIA escribiéndole al padre o la madre un resumen semanal " +
            "cálido y honesto sobre cómo les fue a sus hijos. Reglas: tono cercano y humano, español " +
            "rioplatense, SIN tecnicismos y SIN markdown (texto plano, sin asteriscos, sin viñetas, sin " +
            "títulos). Un párrafo breve por hijo, nombrándolo: qué trabajó, un logro concreto y, si se " +
            "trabó en algo, decilo con cariño. Si un hijo no estudió esta semana, mencionalo con suavidad " +
            "como un recordatorio, sin culpar a nadie. Cerrá con una frase corta de aliento. No inventes " +
            "datos que no estén en la información que te paso.";

        var userMsg = $"Información de la semana (no la repitas literal, contala natural):\n\n{childrenData}";

        var body = JsonSerializer.Serialize(new
        {
            model = ClaudeModel,
            max_tokens = 800,
            system,
            messages = new[] { new { role = "user", content = userMsg } }
        });

        var client = _httpClientFactory.CreateClient("anthropic");
        var response = await client.PostAsync("/v1/messages",
            new StringContent(body, Encoding.UTF8, "application/json"), ct);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var root = doc.RootElement;
        return root.GetProperty("content")[0].GetProperty("text").GetString() ?? string.Empty;
    }

    private static string BuildEmailHtml(string body)
    {
        var paragraphs = string.Join("", body
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => $"<p style=\"margin:0 0 1rem;line-height:1.55;\">{System.Net.WebUtility.HtmlEncode(p.Trim())}</p>"));

        return $"""
            <div style="font-family:system-ui,-apple-system,'Segoe UI',Roboto,sans-serif;max-width:560px;margin:0 auto;color:#2a2a2a;">
              <h2 style="color:#5C7A5E;font-size:1.15rem;margin:0 0 1rem;">Cómo fue la semana</h2>
              {paragraphs}
              <p style="margin-top:2rem;font-size:.82rem;color:#999;">
                Te llega porque activaste el resumen semanal en tu panel de miTutorIA.
                Podés desactivarlo cuando quieras desde el panel.<br>
                miTutorIA · <a href="https://mitutoria.app" style="color:#5C7A5E;">mitutoria.app</a>
              </p>
            </div>
            """;
    }
}
