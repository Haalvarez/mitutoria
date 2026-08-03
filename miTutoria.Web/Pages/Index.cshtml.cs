using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;
using miTutoria.Web.Data.Entities.Auth;
using miTutoria.Web.Infrastructure;

namespace miTutoria.Web.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _dbContext;
    private readonly TelegramService _telegram;
    public IndexModel(AppDbContext dbContext, TelegramService telegram)
    {
        _dbContext = dbContext;
        _telegram  = telegram;
    }

    public bool WaitlistSent { get; private set; }

    public async Task OnGetAsync()
    {
        WaitlistSent = Request.Query.ContainsKey("joined");
        await RecordHitAsync();
    }

    // Registra una visita humana a la landing (sin cookies). Nunca rompe el render.
    private async Task RecordHitAsync()
    {
        try
        {
            var ua = Request.Headers.UserAgent.ToString();
            var uaLower = ua.ToLowerInvariant();

            // Mismo criterio que la analítica de la agenda: descartamos crawlers
            // y el preview de WhatsApp (que NO es una visita humana real).
            var isBot = string.IsNullOrEmpty(uaLower) || uaLower.Contains("bot") || uaLower.Contains("crawl")
                        || uaLower.Contains("spider") || uaLower.Contains("preview")
                        || uaLower.Contains("facebookexternalhit") || uaLower.Contains("whatsapp");
            if (isBot) return;

            var isMobile = uaLower.Contains("mobi") || uaLower.Contains("android")
                           || uaLower.Contains("iphone") || uaLower.Contains("ipad");

            _dbContext.LandingHits.Add(new Data.Entities.LandingHit
            {
                Path     = Request.Path.Value,
                Referrer = Request.Headers.Referer.ToString() is { Length: > 0 } r ? r : null,
                UserAgent = ua is { Length: > 0 } ? (ua.Length > 400 ? ua[..400] : ua) : null,
                IsMobile = isMobile
            });
            await _dbContext.SaveChangesAsync();
        }
        catch { /* la analítica nunca rompe la landing */ }
    }

    public async Task<IActionResult> OnPostWaitlistAsync(string email, string? name, string? phone, string? website, long ts = 0)
    {
        // Anti-bot inocuo (invisible para el usuario real):
        //  a) Honeypot: si el campo "website" viene completo, lo llenó un bot.
        //  b) Time-check: un POST en menos de ~2,5 s desde el render es automático.
        // En ambos casos fingimos éxito para que el bot no reintente, pero NO guardamos nada.
        var elapsedMs = ts > 0 ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - ts : long.MaxValue;
        if (!string.IsNullOrWhiteSpace(website) || elapsedMs < 2500)
            return RedirectToPage(new { joined = true });

        // Nombre y email son obligatorios; el teléfono (WhatsApp) es opcional.
        if (string.IsNullOrWhiteSpace(name)
            || string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return RedirectToPage();

        // Acotar longitudes: evita filas basura gigantes y mensajes de Telegram enormes.
        name  = name.Trim();  if (name.Length  > 100) name  = name[..100];
        email = email.Trim().ToLower();  if (email.Length > 254) email = email[..254];
        phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        if (phone is { Length: > 30 }) phone = phone[..30];

        var exists = await _dbContext.WaitlistEntries.AnyAsync(w => w.Email == email);
        if (!exists)
        {
            _dbContext.WaitlistEntries.Add(new WaitlistEntry
            {
                Email = email,
                Name  = name,
                Phone = phone
            });
            await _dbContext.SaveChangesAsync();

            // Escapar antes de interpolar en el mensaje HTML (un nombre con '<' rompía o spoofeaba la notificación).
            var phoneLine = phone is null ? string.Empty : $"\n📱 {TelegramService.EscapeHtml(phone)}";
            var total = await _dbContext.WaitlistEntries.CountAsync();
            await _telegram.SendAsync($"🙋 Nueva inscripción en waitlist\n<b>{TelegramService.EscapeHtml(name)} ({TelegramService.EscapeHtml(email)})</b>{phoneLine}\nTotal: {total}");
        }

        return RedirectToPage(new { joined = true });
    }
}
