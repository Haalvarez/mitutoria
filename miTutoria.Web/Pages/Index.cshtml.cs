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

    public void OnGet()
    {
        WaitlistSent = Request.Query.ContainsKey("joined");
    }

    public async Task<IActionResult> OnPostWaitlistAsync(string email, string? name)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return RedirectToPage();

        var exists = _dbContext.WaitlistEntries.Any(w => w.Email == email.Trim().ToLower());
        if (!exists)
        {
            _dbContext.WaitlistEntries.Add(new WaitlistEntry
            {
                Email = email.Trim().ToLower(),
                Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim()
            });
            await _dbContext.SaveChangesAsync();

            var displayName = string.IsNullOrWhiteSpace(name) ? email : $"{name} ({email})";
            var total = _dbContext.WaitlistEntries.Count();
            await _telegram.SendAsync($"🙋 Nueva inscripción en waitlist\n<b>{displayName}</b>\nTotal: {total}");
        }

        return RedirectToPage(new { joined = true });
    }
}
