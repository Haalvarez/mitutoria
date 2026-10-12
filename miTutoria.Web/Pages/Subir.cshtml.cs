using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;
using miTutoria.Web.Data.Entities.Auth;

namespace miTutoria.Web.Pages;

/// <summary>
/// Subir apuntes desde el celular, sin el chat: hijo → materia → fotos.
/// Con sesión de padre/madre se elige el hijo; con sesión de alumno va directo a sus materias.
/// La subida en sí la hace el handler SaveMaterial del aula (mismo OCR y contabilidad).
/// </summary>
public class SubirModel : PageModel
{
    private readonly AppDbContext _dbContext;

    public SubirModel(AppDbContext dbContext) => _dbContext = dbContext;

    public record KidInfo(int Id, string Name, string? Avatar);
    public record SubjectInfo(int Id, string Name, bool HasMaterial);

    public List<KidInfo> Kids { get; private set; } = [];
    public KidInfo? Kid { get; private set; }
    public List<SubjectInfo> Subjects { get; private set; } = [];
    public SubjectInfo? Subject { get; private set; }
    public bool IsParent { get; private set; }

    public async Task<IActionResult> OnGetAsync(int? s, int? c)
    {
        var kids = await AllowedKidsAsync();
        if (kids is null) return RedirectToPage("/Login");

        Kids = kids;
        // Un solo hijo (o sesión de alumno): no hace falta elegir.
        Kid = kids.FirstOrDefault(k => k.Id == s) ?? (kids.Count == 1 ? kids[0] : null);
        if (Kid is null) return Page();

        Subjects = await _dbContext.Classrooms
            .Where(x => x.StudentId == Kid.Id)
            .OrderBy(x => x.Name)
            .Select(x => new SubjectInfo(x.Id, x.Name, x.Material != null && x.Material != ""))
            .ToListAsync();
        Subject = Subjects.FirstOrDefault(x => x.Id == c);
        return Page();
    }

    public async Task<IActionResult> OnPostCreateSubjectAsync(int studentId, string? subjectName)
    {
        var kids = await AllowedKidsAsync();
        if (kids is null) return RedirectToPage("/Login");
        if (kids.All(k => k.Id != studentId)) return RedirectToPage();

        var name = string.IsNullOrWhiteSpace(subjectName) ? "Nueva materia" : subjectName.Trim();
        if (name.Length > 40) name = name[..40];

        // Misma regla anti-duplicados que el aula: si ya existe con ese nombre, se usa esa.
        var existing = (await _dbContext.Classrooms.Where(x => x.StudentId == studentId).ToListAsync())
            .FirstOrDefault(x => string.Equals(x.Name.Trim(), name, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            existing = new Data.Entities.Academic.Classroom
            {
                StudentId = studentId,
                Name = name,
                Mode = Classroom.IndexModel.InferMode(name),
                SystemPrompt = string.Empty,
                LastActiveAt = DateTime.UtcNow
            };
            _dbContext.Classrooms.Add(existing);
            await _dbContext.SaveChangesAsync();
        }

        return RedirectToPage(new { s = studentId, c = existing.Id });
    }

    // Hijos que esta sesión puede tocar: el propio alumno, o todos los de la familia. null = sin sesión.
    private async Task<List<KidInfo>?> AllowedKidsAsync()
    {
        var studentSessionId = HttpContext.Session.GetInt32("StudentId");
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (studentSessionId is null && familyId is null) return null;

        IsParent = studentSessionId is null;
        var query = _dbContext.Users.Where(u => u.Role == UserRole.Student);
        query = studentSessionId.HasValue
            ? query.Where(u => u.Id == studentSessionId.Value)
            : query.Where(u => u.FamilyId == familyId!.Value);

        return await query
            .OrderBy(u => u.FullName)
            .Select(u => new KidInfo(u.Id, u.Nickname ?? u.FullName, u.Avatar))
            .ToListAsync();
    }
}
