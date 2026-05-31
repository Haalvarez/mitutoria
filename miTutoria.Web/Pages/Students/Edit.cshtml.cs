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
            return RedirectToPage("/Dashboard");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Error: {ex.GetType().Name} — {ex.Message}");
            LoadFromStudent(student);
            return Page();
        }
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
