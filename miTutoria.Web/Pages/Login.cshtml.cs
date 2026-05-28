using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;
using miTutoria.Web.Data.Entities.Auth;
using Resend;

namespace miTutoria.Web.Pages;

public class LoginModel : PageModel
{
    private readonly AppDbContext _dbContext;
    private readonly IResend _resend;

    public LoginModel(AppDbContext dbContext, IResend resend)
    {
        _dbContext = dbContext;
        _resend = resend;
    }

    public bool Sent { get; private set; }
    public bool Expired { get; private set; }
    public string Email { get; private set; } = string.Empty;

    public void OnGet(bool sent = false, bool expired = false)
    {
        Sent = sent;
        Expired = expired;
    }

    public async Task<IActionResult> OnPostAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError(string.Empty, "El email es obligatorio.");
            return Page();
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        Email = normalizedEmail;

        var family = await _dbContext.Families.SingleOrDefaultAsync(f => f.Email == normalizedEmail);
        if (family is null)
        {
            family = new Family
            {
                Email = normalizedEmail,
                Name = normalizedEmail
            };
            _dbContext.Families.Add(family);
        }

        var token = Guid.NewGuid().ToString("N");
        family.MagicToken = token;
        family.MagicTokenExpiry = DateTime.UtcNow.AddMinutes(15);

        await _dbContext.SaveChangesAsync();

        var url = $"{Request.Scheme}://{Request.Host}/auth/verify?token={token}";
        var message = new EmailMessage
        {
            From = "noreply@mitutoria.app",
            Subject = "Tu acceso a miTutorIA",
            HtmlBody = $"Hacé click aquí para entrar: <a href='{url}'>{url}</a>"
        };
        message.To.Add(normalizedEmail);

        await _resend.EmailSendAsync(message);

        return Redirect("/Login?sent=true");
    }
}
