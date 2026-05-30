using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;
using miTutoria.Web.Data.Entities.Academic;
using miTutoria.Web.Data.Entities.Auth;

namespace miTutoria.Web.Pages.Classroom;

public class IndexModel : PageModel
{
    private readonly AppDbContext _dbContext;

    public IndexModel(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public int StudentId { get; private set; }
    public string StudentName { get; private set; } = string.Empty;
    public List<Message> Messages { get; private set; } = new();

    [BindProperty]
    public string Content { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(int studentId)
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return RedirectToPage("/Login");

        var student = await _dbContext.Users
            .SingleOrDefaultAsync(u => u.Id == studentId && u.FamilyId == familyId.Value && u.Role == UserRole.Student);

        if (student is null) return RedirectToPage("/Dashboard");

        StudentId = student.Id;
        StudentName = student.Nickname ?? student.FullName;

        var classroom = await GetOrCreateClassroomAsync(studentId);
        Messages = await _dbContext.Messages
            .Where(m => m.ClassroomId == classroom.Id)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int studentId)
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return RedirectToPage("/Login");

        var student = await _dbContext.Users
            .SingleOrDefaultAsync(u => u.Id == studentId && u.FamilyId == familyId.Value && u.Role == UserRole.Student);

        if (student is null) return RedirectToPage("/Dashboard");

        StudentId = student.Id;
        StudentName = student.Nickname ?? student.FullName;

        if (string.IsNullOrWhiteSpace(Content))
        {
            ModelState.AddModelError(nameof(Content), "El mensaje no puede estar vacío.");
            var classroom2 = await GetOrCreateClassroomAsync(studentId);
            Messages = await _dbContext.Messages
                .Where(m => m.ClassroomId == classroom2.Id)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
            return Page();
        }

        try
        {
            var classroom = await GetOrCreateClassroomAsync(studentId);

            _dbContext.Messages.Add(new Message
            {
                ClassroomId = classroom.Id,
                Role = MessageRole.User,
                Content = Content.Trim()
            });

            await _dbContext.SaveChangesAsync();

            return RedirectToPage(new { studentId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Error: {ex.GetType().Name} — {ex.Message}");
            var classroom = await GetOrCreateClassroomAsync(studentId);
            Messages = await _dbContext.Messages
                .Where(m => m.ClassroomId == classroom.Id)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
            return Page();
        }
    }

    private async Task<Data.Entities.Academic.Classroom> GetOrCreateClassroomAsync(int studentId)
    {
        var classroom = await _dbContext.Classrooms
            .SingleOrDefaultAsync(c => c.StudentId == studentId);

        if (classroom is null)
        {
            classroom = new Data.Entities.Academic.Classroom
            {
                StudentId = studentId,
                SubjectId = null,
                SystemPrompt = string.Empty
            };
            _dbContext.Classrooms.Add(classroom);
            await _dbContext.SaveChangesAsync();
        }

        return classroom;
    }
}
