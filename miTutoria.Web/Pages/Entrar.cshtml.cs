using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;
using miTutoria.Web.Data.Entities.Auth;

namespace miTutoria.Web.Pages;

public class EntrarModel : PageModel
{
    private readonly AppDbContext _dbContext;

    public EntrarModel(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [BindProperty] public string Username { get; set; } = string.Empty;
    [BindProperty] public string Pin { get; set; } = string.Empty;

    public IActionResult OnGet()
    {
        if (HttpContext.Session.GetInt32("StudentId") is not null)
            return RedirectToStudentClassroom();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var username = Username.Trim().ToLowerInvariant();

        var student = await _dbContext.Users
            .SingleOrDefaultAsync(u =>
                u.Role == UserRole.Student &&
                u.StudentUsername != null &&
                u.StudentUsername == username);

        if (student is null || string.IsNullOrEmpty(student.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Usuario o PIN incorrecto.");
            return Page();
        }

        var hasher = new PasswordHasher<User>();
        var result = hasher.VerifyHashedPassword(student, student.PasswordHash, Pin);

        if (result == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Usuario o PIN incorrecto.");
            return Page();
        }

        HttpContext.Session.SetInt32("StudentId", student.Id);
        HttpContext.Session.SetInt32("StudentFamilyId", student.FamilyId);
        HttpContext.Session.Remove("FamilyId"); // limpiar sesión de padre si había

        return RedirectToPage("/Classroom/Index", new { studentId = student.Id });
    }

    private IActionResult RedirectToStudentClassroom()
    {
        var studentId = HttpContext.Session.GetInt32("StudentId")!.Value;
        return RedirectToPage("/Classroom/Index", new { studentId });
    }
}
