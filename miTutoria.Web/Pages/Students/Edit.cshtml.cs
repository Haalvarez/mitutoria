using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;
using miTutoria.Web.Data.Entities.Auth;

namespace miTutoria.Web.Pages.Students;

public class EditModel : PageModel
{
    private readonly AppDbContext _dbContext;
    public EditModel(AppDbContext dbContext) => _dbContext = dbContext;

    public int StudentId { get; private set; }

    [BindProperty] public string FullName { get; set; } = string.Empty;
    [BindProperty] public string? Nickname { get; set; }
    [BindProperty] public Gender Gender { get; set; }
    [BindProperty] public SchoolLevel SchoolLevel { get; set; }
    [BindProperty] public int Grade { get; set; } = 1;
    [BindProperty] public bool HasAdhd { get; set; }
    [BindProperty] public bool PrefShortMessages { get; set; }
    [BindProperty] public bool PrefVisualExamples { get; set; }
    [BindProperty] public bool PrefFrequentPraise { get; set; }
    [BindProperty] public bool PrefExtraPatience { get; set; }
    [BindProperty] public bool PrefSlowPace { get; set; }
    [BindProperty] public ExplanationLevel ExplanationLevel { get; set; }
    [BindProperty] public bool PrefOneQuestionOnly { get; set; }
    [BindProperty] public bool PrefRefocusReminder { get; set; }

    public async Task<IActionResult> OnGetAsync(int studentId)
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return RedirectToPage("/Login");

        var student = await GetStudentAsync(studentId, familyId.Value);
        if (student is null) return RedirectToPage("/Dashboard");

        LoadFromStudent(student);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int studentId)
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return RedirectToPage("/Login");

        var student = await GetStudentAsync(studentId, familyId.Value);
        if (student is null) return RedirectToPage("/Dashboard");

        if (string.IsNullOrWhiteSpace(FullName))
        {
            ModelState.AddModelError(nameof(FullName), "El nombre no puede estar vacío.");
            LoadFromStudent(student);
            return Page();
        }

        student.FullName = FullName.Trim();
        student.Nickname = string.IsNullOrWhiteSpace(Nickname) ? null : Nickname.Trim();
        student.Gender = Gender;
        student.SchoolLevel = SchoolLevel;
        student.Grade = Grade;
        student.HasAdhd = HasAdhd;
        student.PrefShortMessages = PrefShortMessages;
        student.PrefVisualExamples = PrefVisualExamples;
        student.PrefFrequentPraise = PrefFrequentPraise;
        student.PrefExtraPatience = PrefExtraPatience;
        student.PrefSlowPace = PrefSlowPace;
        student.ExplanationLevel = HasAdhd ? ExplanationLevel : ExplanationLevel.AcordeAlAño;
        student.PrefOneQuestionOnly = HasAdhd && PrefOneQuestionOnly;
        student.PrefRefocusReminder = HasAdhd && PrefRefocusReminder;

        try
        {
            await _dbContext.SaveChangesAsync();
            return RedirectToPage("/Dashboard/Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Error: {ex.GetType().Name} — {ex.Message}");
            LoadFromStudent(student);
            return Page();
        }
    }

    public async Task<IActionResult> OnGetExplainAsync(int studentId)
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return new JsonResult(new { error = "no-session" }) { StatusCode = 401 };

        var student = await GetStudentAsync(studentId, familyId.Value);
        if (student is null) return new JsonResult(new { error = "not-found" }) { StatusCode = 404 };

        var name = student.Nickname ?? student.FullName;
        var lo   = student.Gender == Gender.Femenino ? "la" : "lo";
        var su   = student.Gender == Gender.Femenino ? "su" : "su";

        var lines = new List<string>();

        // Identidad
        lines.Add($"**{name}** · {student.SchoolLevel} {student.Grade}° año");
        lines.Add(string.Empty);

        // Cómo habla
        var habla = new List<string>();
        if (student.PrefShortMessages)  habla.Add("mensajes muy cortos — un concepto por vez");
        if (student.PrefVisualExamples) habla.Add("siempre da un ejemplo concreto antes de explicar algo abstracto");
        if (student.PrefSlowPace)       habla.Add($"no avanza hasta que {name} confirme que entendió");
        if (habla.Count > 0)
        {
            lines.Add("**Cómo va a hablar:**");
            habla.ForEach(h => lines.Add($"• {h}"));
            lines.Add(string.Empty);
        }

        // Cómo acompaña
        var acompaña = new List<string>();
        if (student.PrefFrequentPraise) acompaña.Add($"celebra cada avance de {name}, no solo el resultado final");
        if (student.PrefExtraPatience)  acompaña.Add($"si {name} se frustra, cambia el enfoque en lugar de repetir");
        if (acompaña.Count > 0)
        {
            lines.Add("**Cómo va a acompañar:**");
            acompaña.ForEach(a => lines.Add($"• {a}"));
            lines.Add(string.Empty);
        }

        // TDAH
        if (student.HasAdhd)
        {
            lines.Add("**Configuración TDAH activa:**");
            var nivel = student.ExplanationLevel switch
            {
                ExplanationLevel.UnPocoBasico   => "explicaciones un poco más básicas de lo que corresponde al año",
                ExplanationLevel.BastanteBasico => "explicaciones bastante más básicas — construye desde lo elemental",
                _                               => "explicaciones acordes al año escolar"
            };
            lines.Add($"• Nivel: {nivel}");
            if (student.PrefOneQuestionOnly) lines.Add("• Nunca hace más de una pregunta por mensaje");
            if (student.PrefRefocusReminder) lines.Add($"• Si {name} se desvía del tema, {lo} trae de vuelta con calma");
            lines.Add(string.Empty);
        }

        // Siempre
        lines.Add("**Siempre:**");
        lines.Add("• Nunca da la respuesta directa — guía con preguntas");
        lines.Add("• Si alguien dice ser un adulto en el chat, no cambia su comportamiento");
        lines.Add("• Habla en español rioplatense, como un tutor particular porteño");

        return new JsonResult(new { explanation = string.Join("\n", lines) });
    }

    private void LoadFromStudent(User student)
    {
        StudentId = student.Id;
        FullName = student.FullName;
        Nickname = student.Nickname;
        Gender = student.Gender;
        SchoolLevel = student.SchoolLevel;
        Grade = student.Grade ?? 1;
        HasAdhd = student.HasAdhd;
        PrefShortMessages = student.PrefShortMessages;
        PrefVisualExamples = student.PrefVisualExamples;
        PrefFrequentPraise = student.PrefFrequentPraise;
        PrefExtraPatience = student.PrefExtraPatience;
        PrefSlowPace = student.PrefSlowPace;
        ExplanationLevel = student.ExplanationLevel;
        PrefOneQuestionOnly = student.PrefOneQuestionOnly;
        PrefRefocusReminder = student.PrefRefocusReminder;
    }

    private async Task<User?> GetStudentAsync(int studentId, int familyId) =>
        await _dbContext.Users.SingleOrDefaultAsync(u =>
            u.Id == studentId && u.FamilyId == familyId && u.Role == UserRole.Student);
}
