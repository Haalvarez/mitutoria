using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

    public async Task<IActionResult> OnPostWaitlistAsync(string email, string? name, string? phone)
    {
        // Nombre y email son obligatorios; el teléfono (WhatsApp) es opcional.
        if (string.IsNullOrWhiteSpace(name)
            || string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return RedirectToPage();

        var exists = _dbContext.WaitlistEntries.Any(w => w.Email == email.Trim().ToLower());
        if (!exists)
        {
            _dbContext.WaitlistEntries.Add(new WaitlistEntry
            {
                Email = email.Trim().ToLower(),
                Name  = name.Trim(),
                Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim()
            });
            await _dbContext.SaveChangesAsync();

            var phoneLine = string.IsNullOrWhiteSpace(phone) ? string.Empty : $"\n📱 {phone.Trim()}";
            var total = _dbContext.WaitlistEntries.Count();
            await _telegram.SendAsync($"🙋 Nueva inscripción en waitlist\n<b>{name.Trim()} ({email})</b>{phoneLine}\nTotal: {total}");
        }

        return RedirectToPage(new { joined = true });
    }
}
