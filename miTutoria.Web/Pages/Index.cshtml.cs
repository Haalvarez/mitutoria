using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using miTutoria.Web.Data;
using miTutoria.Web.Data.Entities.Auth;

namespace miTutoria.Web.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _dbContext;
    public IndexModel(AppDbContext dbContext) => _dbContext = dbContext;

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
        }

        return RedirectToPage(new { joined = true });
    }
}
