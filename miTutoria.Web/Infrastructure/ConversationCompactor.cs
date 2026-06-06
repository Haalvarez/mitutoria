using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;
using miTutoria.Web.Data.Entities.Academic;
using miTutoria.Web.Data.Entities.Billing;

namespace miTutoria.Web.Infrastructure;

/// <summary>
/// Compacta conversaciones largas E inactivas: resume con Claude, guarda el resumen
/// (integrando el previo) y borra los mensajes. Lo dispara el scheduler — el alumno
/// NO ve ningún botón (es plumbing técnico). Nunca compacta sesiones activas.
/// </summary>
public class ConversationCompactor
{
    private const string ClaudeModel = "claude-haiku-4-5-20251001";
    private const decimal CostPerInputToken  = 0.80m / 1_000_000;
    private const decimal CostPerOutputToken = 4.00m / 1_000_000;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ExchangeRateService _exchangeRate;
    private readonly ILogger<ConversationCompactor> _logger;

    public ConversationCompactor(IHttpClientFactory httpClientFactory, IConfiguration config,
        ExchangeRateService exchangeRate, ILogger<ConversationCompactor> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _exchangeRate = exchangeRate;
        _logger = logger;
    }

    /// <summary>
    /// Busca conversaciones largas (≥ COMPACT_MIN_MESSAGES) e inactivas
    /// (sin mensajes hace ≥ COMPACT_INACTIVE_HOURS) y las compacta. Devuelve cuántas compactó.
    /// </summary>
    public async Task<int> RunAsync(AppDbContext db, CancellationToken ct)
    {
        var minMessages   = _config.GetValue("COMPACT_MIN_MESSAGES", 30);
        var inactiveHours = _config.GetValue("COMPACT_INACTIVE_HOURS", 12);
        var cutoff = DateTime.UtcNow.AddHours(-inactiveHours);

        // Largas y con TODA la actividad antes del corte (= sesión terminada).
        var candidates = await db.Classrooms
            .Where(c => c.Messages.Count >= minMessages && c.Messages.All(m => m.CreatedAt < cutoff))
            .Include(c => c.Messages)
            .Take(20)   // tope por pasada, para no abusar de la API
            .ToListAsync(ct);

        if (candidates.Count == 0) return 0;

        // Datos del alumno (nombre + familia) para el transcript y el costo.
        var studentIds = candidates.Select(c => c.StudentId).Distinct().ToList();
        var students = await db.Users
            .Where(u => studentIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FamilyId, Name = u.Nickname ?? u.FullName })
            .ToDictionaryAsync(u => u.Id, ct);

        var arsRate = await _exchangeRate.GetMepRateAsync();
        var compacted = 0;

        foreach (var classroom in candidates)
        {
            if (!students.TryGetValue(classroom.StudentId, out var stu)) continue;
            try
            {
                var history = classroom.Messages.OrderBy(m => m.CreatedAt).ToList();
                var (summary, tIn, tOut) = await SummarizeAsync(stu.Name, classroom.CompactSummary, history, ct);
                if (string.IsNullOrWhiteSpace(summary)) continue;

                classroom.CompactSummary = summary;
                db.Messages.RemoveRange(classroom.Messages);
                db.TokenEvents.Add(new TokenEvent
                {
                    FamilyId = stu.FamilyId,
                    UserId = classroom.StudentId,
                    ModelUsed = ClaudeModel,
                    Feature = "compact-auto",
                    TokensIn = tIn,
                    TokensOut = tOut,
                    CostUsd = tIn * CostPerInputToken + tOut * CostPerOutputToken,
                    ArsRate = arsRate
                });
                await db.SaveChangesAsync(ct);
                compacted++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Compactor: falló al compactar el aula {ClassroomId}", classroom.Id);
            }
        }

        return compacted;
    }

    private async Task<(string summary, int tIn, int tOut)> SummarizeAsync(
        string studentName, string? priorSummary, List<Message> history, CancellationToken ct)
    {
        var transcript = string.Join("\n", history.Select(m =>
            $"{(m.Role == MessageRole.User ? studentName : "Tutor")}: {m.Content}"));

        var prior = string.IsNullOrWhiteSpace(priorSummary)
            ? string.Empty
            : $"Resumen previo de la sesión (integralo):\n{priorSummary}\n\n";

        var userMsg = $"Resumí esta sesión de tutoría en 4-6 líneas, destacando qué temas se trabajaron " +
                      $"y qué logró el estudiante. Integrá el resumen previo si lo hay.\n\n{prior}{transcript}";

        var body = JsonSerializer.Serialize(new
        {
            model = ClaudeModel,
            max_tokens = 512,
            system = "Sos un asistente que genera resúmenes concisos de sesiones de tutoría. Respondé solo con el resumen, sin saludos ni explicaciones.",
            messages = new[] { new { role = "user", content = userMsg } }
        });

        var client = _httpClientFactory.CreateClient("anthropic");
        var response = await client.PostAsync("/v1/messages",
            new StringContent(body, Encoding.UTF8, "application/json"), ct);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var root = doc.RootElement;
        var summary = root.GetProperty("content")[0].GetProperty("text").GetString() ?? string.Empty;
        var tIn  = root.GetProperty("usage").GetProperty("input_tokens").GetInt32();
        var tOut = root.GetProperty("usage").GetProperty("output_tokens").GetInt32();
        return (summary, tIn, tOut);
    }
}
