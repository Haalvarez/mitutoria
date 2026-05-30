using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;
using miTutoria.Web.Data.Entities.Auth;

namespace miTutoria.Web.Pages.Dashboard;

public class IndexModel : PageModel
{
    private readonly AppDbContext _dbContext;

    public IndexModel(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public string FamilyName { get; private set; } = string.Empty;
    public List<User> Students { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null)
        {
            return RedirectToPage("/Login");
        }

        var family = await _dbContext.Families
            .Include(f => f.Users.Where(u => u.Role == UserRole.Student))
            .SingleOrDefaultAsync(f => f.Id == familyId.Value);

        if (family is null)
        {
            return RedirectToPage("/Login");
        }

        FamilyName = family.Nickname ?? family.Name ?? family.Email;
        Students = family.Users
            .OrderBy(u => u.FullName)
            .ToList();

        return Page();
    }
}
