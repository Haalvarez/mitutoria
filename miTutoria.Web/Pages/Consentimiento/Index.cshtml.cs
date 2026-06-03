using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using miTutoria.Web.Data;

namespace miTutoria.Web.Pages.Consentimiento;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public async Task<IActionResult> OnGetAsync()
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return RedirectToPage("/Login");

        var family = await _db.Families.FindAsync(familyId.Value);
        if (family is null) return RedirectToPage("/Login");
        if (family.ConsentAt is not null) return RedirectToPage("/Dashboard/Index");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return RedirectToPage("/Login");

        var family = await _db.Families.FindAsync(familyId.Value);
        if (family is null) return RedirectToPage("/Login");

        family.ConsentAt = DateTime.UtcNow;
        family.ConsentIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        family.ConsentVersion = "v1";
        await _db.SaveChangesAsync();

        return RedirectToPage("/Dashboard/Index");
    }
}
