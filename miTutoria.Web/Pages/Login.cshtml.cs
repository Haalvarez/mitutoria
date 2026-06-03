using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;
using Resend;

namespace miTutoria.Web.Pages;

public class LoginModel : PageModel
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _dbContext;
    private readonly IResend _resend;

    public LoginModel(IConfiguration configuration, AppDbContext dbContext, IResend resend)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _resend = resend;
    }

    public bool Sent { get; private set; }
    public bool Expired { get; private set; }
    public string Email { get; private set; } = string.Empty;

    public void OnGet(bool expired = false)
    {
        Sent = Request.Query["sent"] == "true";
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

        var family = await _dbContext.Families.FirstOrDefaultAsync(f => f.Email == normalizedEmail);
        if (family is null || family.SubscriptionStatus == "waitlist")
        {
            ModelState.AddModelError(string.Empty, "Este email no tiene acceso todavía. ¿Querés anotarte en la lista de espera?");
            return Page();
        }

        var token = Guid.NewGuid().ToString("N");
        family.MagicToken = token;
        family.MagicTokenExpiry = DateTime.UtcNow.AddMinutes(15);

        try
        {
            await _dbContext.SaveChangesAsync();

            var baseUrl = _configuration["APP_BASE_URL"] ?? $"{Request.Scheme}://{Request.Host}";
            var url = $"{baseUrl}/Auth/Verify?token={token}";
            var message = new EmailMessage
            {
                From = _configuration["RESEND_FROM"] ?? "noreply@mitutoria.app",
                Subject = "Tu acceso a miTutorIA",
                HtmlBody = $"Hacé click aquí para entrar: <a href='{url}'>{url}</a>"
            };
            message.To.Add(normalizedEmail);

            await _resend.EmailSendAsync(message);

            return Redirect("/Login?sent=true");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Error: {ex.GetType().Name} — {ex.Message}");
            return Page();
        }
    }
}
