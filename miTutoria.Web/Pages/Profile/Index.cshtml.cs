using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;
using miTutoria.Web.Data.Entities.Auth;

namespace miTutoria.Web.Pages.Profile;

public class IndexModel : PageModel
{
    private readonly AppDbContext _dbContext;

    public IndexModel(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public string? Nickname { get; set; }

    [BindProperty]
    public ParentRole ParentRole { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null)
        {
            return RedirectToPage("/Login");
        }

        var family = await _dbContext.Families.SingleOrDefaultAsync(f => f.Id == familyId.Value);
        if (family is null)
        {
            return RedirectToPage("/Login");
        }

        Name = family.Name;
        Nickname = family.Nickname;
        ParentRole = family.ParentRole;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null)
        {
            return RedirectToPage("/Login");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            ModelState.AddModelError(nameof(Name), "El nombre no puede estar vacío.");
            return Page();
        }

        try
        {
            var family = await _dbContext.Families.SingleOrDefaultAsync(f => f.Id == familyId.Value);
            if (family is null)
            {
                return RedirectToPage("/Login");
            }

            family.Name = Name.Trim();
            family.Nickname = string.IsNullOrWhiteSpace(Nickname) ? null : Nickname.Trim();
            family.ParentRole = ParentRole;

            await _dbContext.SaveChangesAsync();

            return RedirectToPage("/Dashboard/Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Error: {ex.GetType().Name} — {ex.Message}");
            return Page();
        }
    }
}
