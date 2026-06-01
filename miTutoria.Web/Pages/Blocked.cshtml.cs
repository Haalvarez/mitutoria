using Microsoft.AspNetCore.Mvc.RazorPages;

namespace miTutoria.Web.Pages;

public class BlockedModel : PageModel
{
    public string Status { get; private set; } = string.Empty;

    public void OnGet(string? status)
    {
        Status = status ?? "suspended";
    }
}
