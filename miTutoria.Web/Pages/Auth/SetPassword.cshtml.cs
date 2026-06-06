using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;
using miTutoria.Web.Data.Entities.Auth;

namespace miTutoria.Web.Pages.Auth;

public class SetPasswordModel : PageModel
{
    private readonly AppDbContext _dbContext;
    public SetPasswordModel(AppDbContext dbContext) => _dbContext = dbContext;

    private const int MinLength = 8;

    [BindProperty] public string Password { get; set; } = string.Empty;
    [BindProperty] public string Confirm { get; set; } = string.Empty;

    // Solo se llega acá tras validar el magic link (que dejó la sesión seteada).
    public IActionResult OnGet()
    {
        if (HttpContext.Session.GetInt32("FamilyId") is null)
            return Redirect("/Login");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return Redirect("/Login");

        if (string.IsNullOrEmpty(Password) || Password.Length < MinLength)
        {
            ModelState.AddModelError(string.Empty, $"La contraseña debe tener al menos {MinLength} caracteres.");
            return Page();
        }
        if (Password != Confirm)
        {
            ModelState.AddModelError(string.Empty, "Las contraseñas no coinciden.");
            return Page();
        }

        var family = await _dbContext.Families.FindAsync(familyId.Value);
        if (family is null) return Redirect("/Login");

        var hasher = new PasswordHasher<Family>();
        family.PasswordHash = hasher.HashPassword(family, Password);
        await _dbContext.SaveChangesAsync();

        if (family.ConsentAt is null)
            return RedirectToPage("/Consentimiento/Index");

        return RedirectToPage("/Dashboard/Index");
    }
}
