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

    public async Task<IActionResult> OnGetAsync(string token)
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

        HttpContext.Session.SetInt32("FamilyId", family.Id);

        return Redirect("/Dashboard");
    }
}
