using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;

namespace miTutoria.Web.Pages.Auth;

public class VerifyModel : PageModel
{
    private readonly AppDbContext _dbContext;

    public VerifyModel(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> OnGetAsync(string token, int reset = 0)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Redirect("/Login?expired=true");
        }

        var family = await _dbContext.Families.SingleOrDefaultAsync(f => f.MagicToken == token);
        if (family is null || family.MagicTokenExpiry is null || family.MagicTokenExpiry < DateTime.UtcNow)
        {
            return Redirect("/Login?expired=true");
        }

        family.MagicToken = null;
        family.MagicTokenExpiry = null;
        await _dbContext.SaveChangesAsync();

        if (!family.IsAccessAllowed)
            return RedirectToPage("/Blocked", new { status = family.SubscriptionStatus });

        HttpContext.Session.SetInt32("FamilyId", family.Id);

        // Magic link = solo onboarding/reset: si no tiene contraseña, o pidió reset, a crearla.
        if (string.IsNullOrEmpty(family.PasswordHash) || reset == 1)
            return RedirectToPage("/Auth/SetPassword");

        if (family.ConsentAt is null)
            return RedirectToPage("/Consentimiento/Index");

        return RedirectToPage("/Dashboard/Index");
    }
}
