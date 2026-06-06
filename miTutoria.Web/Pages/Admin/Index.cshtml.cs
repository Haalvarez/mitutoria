using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;
using miTutoria.Web.Data.Entities.Auth;
using Resend;

namespace miTutoria.Web.Pages.Admin;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IResend _resend;
    private readonly miTutoria.Web.Infrastructure.SchedulerHeartbeat _heartbeat;

    public IndexModel(AppDbContext db, IConfiguration config, IResend resend,
        miTutoria.Web.Infrastructure.SchedulerHeartbeat heartbeat)
    {
        _db = db;
        _config = config;
        _resend = resend;
        _heartbeat = heartbeat;
    }

    // Health-check del scheduler (robot cada 6h).
    public DateTime? SchedulerLastRun { get; private set; }

    // ── Invitación ───────────────────────────────────────────────────────────

    [TempData] public string? InviteResult { get; set; }

    public async Task<IActionResult> OnPostInviteAsync([FromQuery] string? token, string inviteEmail)
    {
        if (!IsAuthorized(token)) return Unauthorized();

        var email = inviteEmail?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            InviteResult = "error:El email es obligatorio.";
            return RedirectToPage(new { token });
        }

        var family = await _db.Families.FirstOrDefaultAsync(f => f.Email == email);
        if (family is null)
        {
            family = new Family { Email = email, Name = email };
            _db.Families.Add(family);
        }

        family.SubscriptionStatus = "trial";
        family.TrialEndsAt = DateTime.UtcNow.AddDays(30);
        family.MagicToken = Guid.NewGuid().ToString("N");
        family.MagicTokenExpiry = DateTime.UtcNow.AddHours(48);
        await _db.SaveChangesAsync();

        var baseUrl = _config["APP_BASE_URL"] ?? $"{Request.Scheme}://{Request.Host}";
        var url = $"{baseUrl}/Auth/Verify?token={family.MagicToken}";
        var message = new EmailMessage
        {
            From = _config["RESEND_FROM"] ?? "noreply@mitutoria.app",
            Subject = "Tu acceso a miTutorIA — piloto cerrado",
            HtmlBody = $"""
                <p>Hola,</p>
                <p>Te invitamos a ser parte del piloto cerrado de <strong>miTutorIA</strong>, un tutor digital con IA pensado para que tus hijos aprendan a pensar, no a copiar.</p>
                <p>Hacé click acá para entrar (el link es válido por 48 horas):</p>
                <p><a href="{url}" style="background:#1a1a1a;color:#fff;padding:.6rem 1.2rem;border-radius:6px;text-decoration:none;display:inline-block;">Entrar a miTutorIA</a></p>
                <p style="margin-top:2rem;font-size:.85rem;color:#888;">Si no pediste esto, ignorá este mail.<br>miTutorIA · <a href="https://mitutoria.app">mitutoria.app</a></p>
                """
        };
        message.To.Add(email);

        try
        {
            await _resend.EmailSendAsync(message);
            InviteResult = $"ok:Invitación enviada a {email}. Trial activo por 30 días.";
        }
        catch (Exception ex)
        {
            InviteResult = $"error:Error al enviar: {ex.Message}";
        }

        return RedirectToPage(new { token });
    }

    // ── Tabla unificada de familias ────────────────────────────────────────────

    public record FamilyRow(
        int Id, string Name, string Email, string Status, bool HasConsented,
        int Students, int Exchanges7d, decimal CostUsd30d,
        DateTime? LastActivity, DateTime? AccessEndsAt,
        bool NearLimit, bool Inactive7Days, bool NoMaterial,
        bool Active, bool Cooling, bool InboxEnabled);

    public List<FamilyRow> Families { get; private set; } = [];

    // ── Scoreboard ─────────────────────────────────────────────────────────────

    public int PilotTotal { get; private set; }
    public int Kr1Active { get; private set; }
    public int Kr2HabitStudents { get; private set; }
    public int Kr2TotalStudents { get; private set; }

    public int RiskNearLimit { get; private set; }
    public int RiskNotConsented { get; private set; }
    public int RiskInactive7 { get; private set; }
    public int RiskNoMaterial { get; private set; }

    private int Kr1MinExchanges => _config.GetValue<int>("PILOT_KR1_MIN_EXCHANGES", 3);
    private int Kr1Threshold => _config.GetValue<int>("PILOT_KR1_THRESHOLD", 6);
    private int Kr2Threshold => _config.GetValue<int>("PILOT_KR2_THRESHOLD", 5);
    private int Kr3Threshold => _config.GetValue<int>("PILOT_KR3_THRESHOLD", 5);
    private int Kr4Threshold => _config.GetValue<int>("PILOT_KR4_THRESHOLD", 3);
    public int[] Thresholds => [Kr1Threshold, Kr2Threshold, Kr3Threshold, Kr4Threshold];

    public List<(string Name, string Status, DateTime? Expiry)> ExpiringFamilies { get; private set; } = [];

    // ── Error log ──────────────────────────────────────────────────────────────

    public record ErrorRow(int Id, DateTime CreatedAt, string Source, string Message, string? Context);
    public List<ErrorRow> RecentErrors { get; private set; } = [];

    // ── Waitlist ─────────────────────────────────────────────────────────────

    public record WaitlistRow(string Email, string? Name, DateTime CreatedAt);
    public List<WaitlistRow> Waitlist { get; private set; } = [];
    public HashSet<string> InvitedEmails { get; private set; } = new();

    // ── Promos (cupones de descuento) ────────────────────────────────────────────

    public record PromoRow(int Id, string Code, string Name, decimal AmountArs,
        DateTime? ValidUntil, bool Active, int? MaxUses, int UsedCount);
    public List<PromoRow> Promos { get; private set; } = [];
    [TempData] public string? PromoResult { get; set; }
    public decimal CuotaArs { get; private set; }

    // ── Plata (todo en USD; el costo es en USD billete) ─────────────────────────

    public decimal TotalCostUsdMonth { get; private set; }    // mes calendario = tu factura Anthropic
    public decimal ProjectionUsdMonth { get; private set; }   // proyección lineal a fin de mes
    public decimal AllTimeCostUsd { get; private set; }       // histórico

    // ── Racha (misma lógica que Classroom) ──────────────────────────────────────
    private static int CalcStreak(IEnumerable<DateTime> dates)
    {
        var today = DateTime.UtcNow.Date;
        var activeDays = dates.Select(d => d.Date).Distinct().OrderByDescending(d => d).ToList();
        if (activeDays.Count == 0) return 0;
        var start = activeDays.Contains(today) ? today : today.AddDays(-1);
        if (!activeDays.Contains(start)) return 0;
        int streak = 0;
        var expected = start;
        foreach (var day in activeDays.Where(d => d <= start).OrderByDescending(d => d))
        {
            if (day == expected) { streak++; expected = expected.AddDays(-1); }
            else break;
        }
        return streak;
    }

    private bool IsAuthorized(string? token)
    {
        var adminToken = _config["ADMIN_TOKEN"];
        return !string.IsNullOrWhiteSpace(adminToken) && token == adminToken;
    }

    // ── GET ──────────────────────────────────────────────────────────────────

    public async Task<IActionResult> OnGetAsync([FromQuery] string? token)
    {
        if (!IsAuthorized(token)) return Unauthorized();

        SchedulerLastRun = _heartbeat.LastRunUtc;

        var now          = DateTime.UtcNow;
        var weekStart    = now.Date.AddDays(-6);
        var monthStart   = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var thirtyAgo    = now.AddDays(-30);
        var queryFrom    = thirtyAgo < monthStart ? thirtyAgo : monthStart;

        var capUsd = _config.GetValue<decimal>("TERMICA_USD", 15m);

        var families = await _db.Families
            .Include(f => f.Users.Where(u => u.Role == Data.Entities.Auth.UserRole.Student))
            .ToListAsync();

        var events = await _db.TokenEvents
            .Where(t => t.CreatedAt >= queryFrom)
            .ToListAsync();

        var classrooms = await _db.Classrooms.ToListAsync();

        Families = families.Select(f =>
        {
            var ev30   = events.Where(e => e.FamilyId == f.Id && e.CreatedAt >= thirtyAgo).ToList();
            var chat7d = events.Count(e => e.FamilyId == f.Id && e.Feature == "chat" && e.CreatedAt >= weekStart);

            var costUsd30   = ev30.Sum(e => e.CostUsd);
            var lastActivity = ev30.Any() ? ev30.Max(e => e.CreatedAt) : (DateTime?)null;
            var noMaterial   = f.Users.Any(u =>
                classrooms.Any(c => c.StudentId == u.Id && string.IsNullOrWhiteSpace(c.Material)));

            var isPilot   = f.SubscriptionStatus is "trial" or "active";
            var active    = chat7d >= Kr1MinExchanges;
            var cooling   = lastActivity.HasValue && (now - lastActivity.Value).TotalDays > 3;

            return new FamilyRow(
                f.Id,
                f.Nickname ?? f.Name ?? f.Email,
                f.Email,
                f.SubscriptionStatus,
                f.ConsentAt.HasValue,
                f.Users.Count,
                chat7d,
                costUsd30,
                lastActivity,
                f.PaidUntil ?? f.TrialEndsAt,
                NearLimit: costUsd30 > capUsd * 0.8m,
                Inactive7Days: isPilot && lastActivity.HasValue && (now - lastActivity.Value).TotalDays > 7,
                NoMaterial: isPilot && noMaterial,
                Active: isPilot && active,
                Cooling: isPilot && cooling,
                InboxEnabled: f.InboxEnabled
            );
        })
        .OrderByDescending(f => f.CostUsd30d)   // money-first: el más caro arriba
        .ToList();

        // Scoreboard
        var pilot = Families.Where(f => f.Status is "trial" or "active").ToList();
        PilotTotal       = pilot.Count;
        Kr1Active        = pilot.Count(f => f.Active);
        RiskNearLimit    = Families.Count(f => f.NearLimit);
        RiskNotConsented = pilot.Count(f => !f.HasConsented);
        RiskInactive7    = pilot.Count(f => f.Inactive7Days);
        RiskNoMaterial   = pilot.Count(f => f.NoMaterial);

        // KR2 — hábito (racha por hijo de las familias del piloto)
        var pilotIds = pilot.Select(f => f.Id).ToHashSet();
        var pilotChat = await _db.TokenEvents
            .Where(t => pilotIds.Contains(t.FamilyId) && t.Feature == "chat" && t.UserId.HasValue)
            .Select(t => new { t.UserId, t.CreatedAt })
            .ToListAsync();
        var pilotStudentIds = families.Where(f => pilotIds.Contains(f.Id))
                                      .SelectMany(f => f.Users).Select(u => u.Id).ToList();
        var streaks = pilotStudentIds
            .Select(id => CalcStreak(pilotChat.Where(e => e.UserId == id).Select(e => e.CreatedAt)))
            .ToList();
        Kr2TotalStudents = streaks.Count;
        Kr2HabitStudents = streaks.Count(s => s >= 7);

        // Vencimientos próximos (≤ 3 días)
        var warnDate = now.AddDays(3);
        ExpiringFamilies = (await _db.Families
            .Where(f => (f.TrialEndsAt != null && f.TrialEndsAt <= warnDate && f.TrialEndsAt >= now)
                     || (f.PaidUntil   != null && f.PaidUntil   <= warnDate && f.PaidUntil   >= now))
            .ToListAsync())
            .Select(f =>
            {
                var expiry = f.PaidUntil ?? f.TrialEndsAt;
                var label  = f.PaidUntil.HasValue ? "pago" : "trial";
                return (f.Nickname ?? f.Name, label, expiry);
            }).ToList();

        RecentErrors = await _db.ErrorLogs
            .OrderByDescending(e => e.CreatedAt)
            .Take(50)
            .Select(e => new ErrorRow(e.Id, e.CreatedAt, e.Source, e.Message, e.Context))
            .ToListAsync();

        Waitlist = await _db.WaitlistEntries
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WaitlistRow(w.Email, w.Name, w.CreatedAt))
            .ToListAsync();

        InvitedEmails = families
            .Where(f => f.SubscriptionStatus is "trial" or "active")
            .Select(f => (f.Email ?? "").ToLowerInvariant())
            .ToHashSet();

        // Promos
        CuotaArs = _config.GetValue<decimal>("CUOTA_ARS", 50000m);
        Promos = await _db.Promos
            .OrderByDescending(p => p.Active).ThenByDescending(p => p.CreatedAt)
            .Select(p => new PromoRow(p.Id, p.Code, p.Name, p.AmountArs,
                p.ValidUntil, p.Active, p.MaxUses, p.UsedCount))
            .ToListAsync();

        // Plata (USD)
        TotalCostUsdMonth  = events.Where(e => e.CreatedAt >= monthStart).Sum(e => e.CostUsd);
        AllTimeCostUsd     = await _db.TokenEvents.SumAsync(e => e.CostUsd);
        var dayOfMonth     = now.Day;
        var daysInMonth    = DateTime.DaysInMonth(now.Year, now.Month);
        ProjectionUsdMonth = dayOfMonth > 0 ? TotalCostUsdMonth / dayOfMonth * daysInMonth : TotalCostUsdMonth;

        return Page();
    }

    // ── Toggle de la Agenda de Classroom (Track 2) por familia ───────────────────

    public async Task<IActionResult> OnPostToggleInboxAsync([FromQuery] string? token, int id)
    {
        if (!IsAuthorized(token)) return Unauthorized();

        var family = await _db.Families.FindAsync(id);
        if (family is not null)
        {
            family.InboxEnabled = !family.InboxEnabled;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { token });
    }

    // ── Promos: crear / activar-desactivar ───────────────────────────────────────

    public async Task<IActionResult> OnPostCreatePromoAsync([FromQuery] string? token,
        string promoCode, string promoName, decimal promoAmount, DateTime? promoValidUntil, int? promoMaxUses)
    {
        if (!IsAuthorized(token)) return Unauthorized();

        var code = Data.Entities.Billing.Promo.Normalize(promoCode);
        if (string.IsNullOrWhiteSpace(code) || promoAmount <= 0)
        {
            PromoResult = "error:Clave e importe (mayor a 0) son obligatorios.";
            return RedirectToPage(new { token });
        }
        if (await _db.Promos.AnyAsync(p => p.Code == code))
        {
            PromoResult = $"error:Ya existe una promo con la clave {code}.";
            return RedirectToPage(new { token });
        }

        _db.Promos.Add(new Data.Entities.Billing.Promo
        {
            Code = code,
            Name = promoName?.Trim() ?? string.Empty,
            AmountArs = promoAmount,
            ValidUntil = promoValidUntil.HasValue
                ? DateTime.SpecifyKind(promoValidUntil.Value, DateTimeKind.Utc) : null,
            MaxUses = promoMaxUses is > 0 ? promoMaxUses : null,
            Active = true
        });
        await _db.SaveChangesAsync();
        PromoResult = $"ok:Promo {code} creada (${promoAmount:N0}).";
        return RedirectToPage(new { token });
    }

    public async Task<IActionResult> OnPostTogglePromoAsync([FromQuery] string? token, int id)
    {
        if (!IsAuthorized(token)) return Unauthorized();

        var promo = await _db.Promos.FindAsync(id);
        if (promo is not null)
        {
            promo.Active = !promo.Active;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { token });
    }

    // ── Detalle de familia (modal ajax) ─────────────────────────────────────────

    public async Task<IActionResult> OnGetDetailAsync([FromQuery] string? token, int id)
    {
        if (!IsAuthorized(token)) return Unauthorized();

        var family = await _db.Families
            .Include(f => f.Users.Where(u => u.Role == Data.Entities.Auth.UserRole.Student))
            .FirstOrDefaultAsync(f => f.Id == id);
        if (family is null) return NotFound();

        var thirtyAgo = DateTime.UtcNow.AddDays(-30);

        var ev30 = await _db.TokenEvents
            .Where(t => t.FamilyId == id && t.CreatedAt >= thirtyAgo)
            .Select(t => new { t.UserId, t.Feature, t.CostUsd, t.CreatedAt })
            .ToListAsync();

        var allChat = await _db.TokenEvents
            .Where(t => t.FamilyId == id && t.Feature == "chat" && t.UserId.HasValue)
            .Select(t => new { t.UserId, t.CreatedAt })
            .ToListAsync();

        var members = family.Users.Select(u => new
        {
            name        = u.Nickname ?? u.FullName,
            exchanges30 = ev30.Count(e => e.UserId == u.Id && e.Feature == "chat"),
            costUsd30   = ev30.Where(e => e.UserId == u.Id).Sum(e => e.CostUsd),
            streak      = CalcStreak(allChat.Where(e => e.UserId == u.Id).Select(e => e.CreatedAt)),
            lastActivity = ev30.Where(e => e.UserId == u.Id).Select(e => (DateTime?)e.CreatedAt).Max()
        }).OrderByDescending(m => m.costUsd30).ToList();

        // Uso nominal por feature (cuántas veces se usó cada cosa)
        var featureUsage = ev30
            .GroupBy(e => e.Feature)
            .OrderByDescending(g => g.Count())
            .Select(g => new { feature = g.Key, count = g.Count() })
            .ToList();

        return new JsonResult(new
        {
            name   = family.Nickname ?? family.Name,
            email  = family.Email,
            status = family.SubscriptionStatus,
            accessEndsAt = family.PaidUntil ?? family.TrialEndsAt,
            members,
            featureUsage
        });
    }
}
