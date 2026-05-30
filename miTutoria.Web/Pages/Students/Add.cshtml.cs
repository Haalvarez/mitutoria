using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using miTutoria.Web.Data;
using miTutoria.Web.Data.Entities.Auth;

namespace miTutoria.Web.Pages.Students;

public class AddModel : PageModel
{
    private readonly AppDbContext _dbContext;

    public AddModel(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [BindProperty]
    public string FullName { get; set; } = string.Empty;

    [BindProperty]
    public string? Nickname { get; set; }

    [BindProperty]
    public int Grade { get; set; } = 1;

    [BindProperty]
    public SchoolLevel SchoolLevel { get; set; }

    [BindProperty]
    public bool HasAdhd { get; set; }

    public IActionResult OnGet()
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return RedirectToPage("/Login");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return RedirectToPage("/Login");

        if (string.IsNullOrWhiteSpace(FullName))
        {
            ModelState.AddModelError(nameof(FullName), "El nombre no puede estar vacío.");
            return Page();
        }

        if ((SchoolLevel == SchoolLevel.Primario && (Grade < 1 || Grade > 7)) ||
            (SchoolLevel == SchoolLevel.Secundario && (Grade < 1 || Grade > 6)))
        {
            ModelState.AddModelError(nameof(Grade), "El año escolar no es válido para el nivel seleccionado.");
            return Page();
        }

        try
        {
            var student = new User
            {
                FullName = FullName.Trim(),
                Nickname = string.IsNullOrWhiteSpace(Nickname) ? null : Nickname.Trim(),
                Grade = Grade,
                SchoolLevel = SchoolLevel,
                HasAdhd = HasAdhd,
                Role = UserRole.Student,
                FamilyId = familyId.Value,
                Email = string.Empty,
                PasswordHash = string.Empty
            };

            _dbContext.Users.Add(student);
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
