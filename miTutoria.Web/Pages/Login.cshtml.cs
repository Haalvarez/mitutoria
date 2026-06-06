using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;
using miTutoria.Web.Data.Entities.Auth;
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

    public bool Sent { get; private set; }      // se mandó el link de crear/resetear contraseña
    public bool Expired { get; private set; }
    public string Email { get; private set; } = string.Empty;

    public void OnGet(bool expired = false)
    {
        Sent = Request.Query["sent"] == "true";
        Expired = expired;
    }

    // Login normal: email + contraseña.
    public async Task<IActionResult> OnPostAsync(string email, string password)
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

        // Todavía no creó contraseña → la mandamos a crearla con el link por email.
        if (string.IsNullOrEmpty(family.PasswordHash))
        {
            ModelState.AddModelError(string.Empty,
                "Todavía no creaste tu contraseña. Usá \"Crear / olvidé mi contraseña\" para recibir el link por email.");
            return Page();
        }

        if (string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError(string.Empty, "Ingresá tu contraseña.");
            return Page();
        }

        var hasher = new PasswordHasher<Family>();
        var result = hasher.VerifyHashedPassword(family, family.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
            return Page();
        }

        if (!family.IsAccessAllowed)
            return RedirectToPage("/Blocked", new { status = family.SubscriptionStatus });

        HttpContext.Session.SetInt32("FamilyId", family.Id);
        HttpContext.Session.Remove("StudentId");   // por si quedó sesión de alumno

        if (family.ConsentAt is null)
            return RedirectToPage("/Consentimiento/Index");

        return RedirectToPage("/Dashboard/Index");
    }

    // "Crear / olvidé mi contraseña": manda el magic link (con reset=1) para setearla.
    public async Task<IActionResult> OnPostForgotAsync(string email)
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
            var url = $"{baseUrl}/Auth/Verify?token={token}&reset=1";
            var message = new EmailMessage
            {
                From = _configuration["RESEND_FROM"] ?? "noreply@mitutoria.app",
                Subject = "Crear tu contraseña de miTutorIA",
                HtmlBody = $"Hacé click para crear o restablecer tu contraseña (válido 15 min): <a href='{url}'>{url}</a>"
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
