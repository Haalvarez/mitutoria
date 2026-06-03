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

    public IndexModel(AppDbContext db, IConfiguration config, IResend resend)
    {
        _db = db;
        _config = config;
        _resend = resend;
    }

    // ── Invitación ───────────────────────────────────────────────────────────

    [TempData] public string? InviteResult { get; set; }

    public async Task<IActionResult> OnPostInviteAsync([FromQuery] string? token, string inviteEmail)
    {
        var adminToken = _config["ADMIN_TOKEN"];
        if (string.IsNullOrWhiteSpace(adminToken) || token != adminToken)
            return Unauthorized();

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

    // ── Familias ─────────────────────────────────────────────────────────────

    public record FamilyRow(
        int Id, string Name,
        int Students,
        int ExchangesToday, int ExchangesWeek, int ExchangesMonth,
        decimal CostUsdMonth, decimal CostArsMonth,
        DateTime? LastActivity,
        bool NearLimit, bool Inactive7Days, bool StudentsWithoutMaterial);

    public List<FamilyRow> Families { get; private set; } = [];

    // ── Monitor de piloto ────────────────────────────────────────────────────

    public record PilotStudentRow(string Name, int Streak);
    public record PilotFamilyRow(
        int Id, string Name, string Email,
        int ChatLast7Days, DateTime? LastActivity,
        bool Cooling,   // > 3 días sin actividad
        bool Active,    // >= KR1MinExchanges en últimos 7 días
        bool HasConsented,
        List<PilotStudentRow> Students);

    public List<PilotFamilyRow> PilotFamilies { get; private set; } = [];
    public int Kr1Active { get; private set; }
    public int Kr2HabitStudents { get; private set; }
    public int Kr2TotalStudents { get; private set; }
    public List<(string Name, int Streak)> Kr2StudentRows { get; private set; } = [];
    public List<(string Name, string Status, DateTime? Expiry)> ExpiringFamilies { get; private set; } = [];

    // KR1: intercambios mínimos en 7 días para considerar familia activa
    private int Kr1MinExchanges => _config.GetValue<int>("PILOT_KR1_MIN_EXCHANGES", 3);
    // Umbrales semáforo (sobre total de cohorte)
    private int Kr1Threshold => _config.GetValue<int>("PILOT_KR1_THRESHOLD", 6);
    private int Kr2Threshold => _config.GetValue<int>("PILOT_KR2_THRESHOLD", 5);
    private int Kr3Threshold => _config.GetValue<int>("PILOT_KR3_THRESHOLD", 5);
    private int Kr4Threshold => _config.GetValue<int>("PILOT_KR4_THRESHOLD", 3);
    public int[] Thresholds => [Kr1Threshold, Kr2Threshold, Kr3Threshold, Kr4Threshold];

    // ── Error log ────────────────────────────────────────────────────────────

    public record ErrorRow(int Id, DateTime CreatedAt, string Source, string Message, string? Context);
    public List<ErrorRow> RecentErrors { get; private set; } = [];

    // ── Waitlist ─────────────────────────────────────────────────────────────

    public record WaitlistRow(string Email, string? Name, DateTime CreatedAt);
    public List<WaitlistRow> Waitlist { get; private set; } = [];
    public HashSet<string> InvitedEmails { get; private set; } = new();   // emails ya en trial/active

    // ── Globales ─────────────────────────────────────────────────────────────

    public decimal TotalCostUsdMonth { get; private set; }
    public decimal TotalCostArsMonth { get; private set; }
    public Dictionary<string, long> TokensByFeature { get; private set; } = [];
    public long MonthlyTokenLimit { get; private set; }

    // ── Saldo de API (estimado) ────────────────────────────────────────────────
    // Anthropic NO expone el saldo prepago por API. Esto es una estimación:
    // crédito cargado a mano (ANTHROPIC_CREDIT_USD) menos el consumo histórico
    // calculado desde token_events. La verdad oficial está en la Console de Anthropic.
    public decimal? ApiCreditUsd { get; private set; }
    public decimal AllTimeCostUsd { get; private set; }
    public decimal? ApiBalanceUsd => ApiCreditUsd.HasValue ? ApiCreditUsd.Value - AllTimeCostUsd : null;

    // ── GET ──────────────────────────────────────────────────────────────────

    public async Task<IActionResult> OnGetAsync([FromQuery] string? token)
    {
        var adminToken = _config["ADMIN_TOKEN"];
        if (string.IsNullOrWhiteSpace(adminToken) || token != adminToken)
            return Unauthorized();

        var now = DateTime.UtcNow;
        var todayStart   = now.Date;
        var weekStart    = todayStart.AddDays(-6);
        var monthStart   = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        MonthlyTokenLimit = _config.GetValue<long>("MONTHLY_TOKEN_LIMIT", 500_000);

        // Todas las familias con sus usuarios
        var families = await _db.Families
            .Include(f => f.Users.Where(u => u.Role == Data.Entities.Auth.UserRole.Student))
            .ToListAsync();

        // Token events del mes
        var events = await _db.TokenEvents
            .Where(t => t.CreatedAt >= monthStart)
            .ToListAsync();

        // Classrooms con material
        var classrooms = await _db.Classrooms.ToListAsync();

        Families = families.Select(f =>
        {
            var fEvents = events.Where(e => e.FamilyId == f.Id).ToList();
            var today   = fEvents.Where(e => e.CreatedAt >= todayStart).ToList();
            var week    = fEvents.Where(e => e.CreatedAt >= weekStart).ToList();

            var chatToday = today.Count(e => e.Feature == "chat");
            var chatWeek  = week.Count(e => e.Feature == "chat");
            var chatMonth = fEvents.Count(e => e.Feature == "chat");

            var costUsd = fEvents.Sum(e => e.CostUsd);
            var costArs = fEvents.Where(e => e.ArsRate.HasValue)
                                 .Sum(e => e.CostUsd * e.ArsRate!.Value);

            var lastActivity = fEvents.Any() ? fEvents.Max(e => e.CreatedAt) : (DateTime?)null;
            var totalTokens  = fEvents.Sum(e => (long)e.TokensIn + e.TokensOut);
            var stuIds       = f.Users.Select(u => u.Id).ToHashSet();
            var studentsWithoutMaterial = f.Users.Any(u =>
                classrooms.Any(c => c.StudentId == u.Id && string.IsNullOrWhiteSpace(c.Material)));

            return new FamilyRow(
                f.Id,
                f.Nickname ?? f.Email,
                f.Users.Count,
                chatToday, chatWeek, chatMonth,
                costUsd, costArs,
                lastActivity,
                NearLimit: totalTokens > MonthlyTokenLimit * 0.8m,
                Inactive7Days: lastActivity.HasValue && (now - lastActivity.Value).TotalDays > 7,
                StudentsWithoutMaterial: studentsWithoutMaterial
            );
        }).OrderByDescending(f => f.LastActivity).ToList();

        // ── Monitor de piloto ────────────────────────────────────────────────
        var pilotFamilies = await _db.Families
            .Include(f => f.Users.Where(u => u.Role == Data.Entities.Auth.UserRole.Student))
            .Where(f => f.SubscriptionStatus == "trial" || f.SubscriptionStatus == "active")
            .ToListAsync();

        var sevenDaysAgo = now.AddDays(-7);
        var allPilotFamilyIds = pilotFamilies.Select(f => f.Id).ToHashSet();
        var allPilotStudentIds = pilotFamilies.SelectMany(f => f.Users).Select(u => u.Id).ToHashSet();

        var pilotEvents = await _db.TokenEvents
            .Where(t => allPilotFamilyIds.Contains(t.FamilyId) && t.Feature == "chat")
            .ToListAsync();

        // Racha por alumno (misma lógica que CalculateStreakAsync en Classroom)
        static int CalcStreak(IEnumerable<DateTime> dates)
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

        PilotFamilies = pilotFamilies.Select(f =>
        {
            var fEvents = pilotEvents.Where(e => e.FamilyId == f.Id).ToList();
            var last7 = fEvents.Where(e => e.CreatedAt >= sevenDaysAgo).Count();
            var lastAct = fEvents.Any() ? fEvents.Max(e => e.CreatedAt) : (DateTime?)null;
            var cooling = lastAct.HasValue && (now - lastAct.Value).TotalDays > 3;
            var active = last7 >= Kr1MinExchanges;

            var students = f.Users.Select(u =>
            {
                var uDates = pilotEvents.Where(e => e.UserId == u.Id).Select(e => e.CreatedAt);
                return new PilotStudentRow(u.Nickname ?? u.FullName, CalcStreak(uDates));
            }).ToList();

            return new PilotFamilyRow(f.Id, f.Nickname ?? f.Name, f.Email,
                last7, lastAct, cooling, active, f.ConsentAt.HasValue, students);
        }).OrderByDescending(f => f.ChatLast7Days).ToList();

        Kr1Active = PilotFamilies.Count(f => f.Active);

        Kr2StudentRows = PilotFamilies
            .SelectMany(f => f.Students)
            .OrderByDescending(s => s.Streak)
            .Select(s => (s.Name, s.Streak))
            .ToList();
        Kr2TotalStudents = Kr2StudentRows.Count;
        Kr2HabitStudents = Kr2StudentRows.Count(s => s.Streak >= 7);

        // Señal de vencimiento (trial_ends_at o paid_until vence en ≤ 3 días)
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

        // Error log — últimos 50
        RecentErrors = await _db.ErrorLogs
            .OrderByDescending(e => e.CreatedAt)
            .Take(50)
            .Select(e => new ErrorRow(e.Id, e.CreatedAt, e.Source, e.Message, e.Context))
            .ToListAsync();

        // Waitlist
        Waitlist = await _db.WaitlistEntries
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WaitlistRow(w.Email, w.Name, w.CreatedAt))
            .ToListAsync();

        InvitedEmails = families
            .Where(f => f.SubscriptionStatus is "trial" or "active")
            .Select(f => (f.Email ?? "").ToLowerInvariant())
            .ToHashSet();

        // Globales
        TotalCostUsdMonth = events.Sum(e => e.CostUsd);
        TotalCostArsMonth = events.Where(e => e.ArsRate.HasValue)
                                  .Sum(e => e.CostUsd * e.ArsRate!.Value);

        // Saldo estimado de API (crédito manual − consumo histórico)
        ApiCreditUsd = _config.GetValue<decimal?>("ANTHROPIC_CREDIT_USD");
        AllTimeCostUsd = await _db.TokenEvents.SumAsync(e => e.CostUsd);
        TokensByFeature = events
            .GroupBy(e => e.Feature)
            .ToDictionary(g => g.Key, g => g.Sum(e => (long)e.TokensIn + e.TokensOut));

        return Page();
    }
}
