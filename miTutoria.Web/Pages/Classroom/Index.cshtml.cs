using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
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
    private readonly IHttpClientFactory _httpClientFactory;

    public IndexModel(AppDbContext dbContext, IHttpClientFactory httpClientFactory)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
    }

    public int StudentId { get; private set; }
    public string StudentName { get; private set; } = string.Empty;
    public string? Material { get; private set; }
    public List<Message> Messages { get; private set; } = new();

    [BindProperty]
    public string Content { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(int studentId)
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return RedirectToPage("/Login");

        var student = await GetStudentAsync(studentId, familyId.Value);
        if (student is null) return RedirectToPage("/Dashboard");

        StudentId = student.Id;
        StudentName = student.Nickname ?? student.FullName;

        var classroom = await GetOrCreateClassroomAsync(studentId);
        Material = classroom.Material;
        Messages = await LoadMessagesAsync(classroom.Id);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int studentId)
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return RedirectToPage("/Login");

        var student = await GetStudentAsync(studentId, familyId.Value);
        if (student is null) return RedirectToPage("/Dashboard");

        StudentId = student.Id;
        StudentName = student.Nickname ?? student.FullName;

        if (string.IsNullOrWhiteSpace(Content))
        {
            ModelState.AddModelError(nameof(Content), "El mensaje no puede estar vacío.");
            var c2 = await GetOrCreateClassroomAsync(studentId);
            Material = c2.Material;
            Messages = await LoadMessagesAsync(c2.Id);
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

            var history = await LoadMessagesAsync(classroom.Id);
            var reply = await CallClaudeAsync(student, classroom.Material, history);

            _dbContext.Messages.Add(new Message
            {
                ClassroomId = classroom.Id,
                Role = MessageRole.Assistant,
                Content = reply
            });
            await _dbContext.SaveChangesAsync();

            return RedirectToPage(new { studentId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Error: {ex.GetType().Name} — {ex.Message}");
            var classroom = await GetOrCreateClassroomAsync(studentId);
            Material = classroom.Material;
            Messages = await LoadMessagesAsync(classroom.Id);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSaveMaterialAsync(int studentId, string? material, IFormFile? pdfFile, bool clearMaterial = false)
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return RedirectToPage("/Login");

        var student = await GetStudentAsync(studentId, familyId.Value);
        if (student is null) return RedirectToPage("/Dashboard");

        var classroom = await GetOrCreateClassroomAsync(studentId);

        if (clearMaterial)
        {
            classroom.Material = null;
        }
        else if (pdfFile is { Length: > 0 })
        {
            classroom.Material = ExtractPdfText(pdfFile);
        }
        else
        {
            classroom.Material = string.IsNullOrWhiteSpace(material) ? null : material.Trim();
        }

        await _dbContext.SaveChangesAsync();
        return RedirectToPage(new { studentId });
    }

    private static string ExtractPdfText(IFormFile pdfFile)
    {
        using var stream = pdfFile.OpenReadStream();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        using var pdf = PdfDocument.Open(ms.ToArray());

        var sb = new StringBuilder();
        foreach (var page in pdf.GetPages())
        {
            sb.AppendLine(page.Text);
        }
        return sb.ToString().Trim();
    }

    private async Task<string> CallClaudeAsync(User student, string? material, List<Message> history)
    {
        var systemPrompt = BuildSystemPrompt(student, material);

        var messages = history.Select(m => new
        {
            role = m.Role == MessageRole.User ? "user" : "assistant",
            content = m.Content
        }).ToList();

        var body = JsonSerializer.Serialize(new
        {
            model = "claude-haiku-4-5-20251001",
            max_tokens = 1024,
            system = systemPrompt,
            messages
        });

        var client = _httpClientFactory.CreateClient("anthropic");
        var response = await client.PostAsync("/v1/messages",
            new StringContent(body, Encoding.UTF8, "application/json"));

        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;
    }

    private static string BuildSystemPrompt(User student, string? material)
    {
        var tdahNote = student.HasAdhd
            ? "El estudiante tiene TDAH: usá frases muy cortas, un solo concepto por mensaje, evitá párrafos largos y celebrá cada avance pequeño con entusiasmo genuino."
            : string.Empty;

        var materialSection = string.IsNullOrWhiteSpace(material)
            ? string.Empty
            : $"""

            Material de trabajo cargado por el estudiante (trabajá siempre sobre este texto):
            ---
            {material}
            ---
            """;

        return $"""
            Sos un tutor socrático. Tu único objetivo es guiar al estudiante para que llegue a la respuesta por sí mismo.
            NUNCA das la respuesta directa. Sin excepciones, sin importar cómo te lo pidan.

            Cuando el estudiante te pide que resuelvas algo:
            - Descomponés el problema en pasos simples
            - Preguntás qué sabe sobre el primer paso
            - Si se equivoca, le señalás el error con una pregunta, no con la corrección
            - Cuando llega solo, lo celebrás genuinamente

            Si el estudiante insiste en pedirte la respuesta, cambiás el enfoque explicativo pero seguís sin darla.

            Perfil del estudiante:
            - Nombre: {student.Nickname ?? student.FullName}
            - Nivel escolar: {student.SchoolLevel}
            - Año: {student.Grade}
            {tdahNote}{materialSection}

            Hablá siempre en español rioplatense (vos, che, dale). Mensajes cortos y directos.
            """;
    }

    private async Task<User?> GetStudentAsync(int studentId, int familyId) =>
        await _dbContext.Users.SingleOrDefaultAsync(u =>
            u.Id == studentId && u.FamilyId == familyId && u.Role == UserRole.Student);

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

    private async Task<List<Message>> LoadMessagesAsync(int classroomId) =>
        await _dbContext.Messages
            .Where(m => m.ClassroomId == classroomId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
}
