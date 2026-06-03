using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;
using miTutoria.Web.Data.Entities.Auth;
using miTutoria.Web.Data.Entities.Billing;

namespace miTutoria.Web.Pages.Students;

public class EditModel : PageModel
{
    private const string ClaudeModel = "claude-haiku-4-5-20251001";
    private const decimal CostPerInputToken  = 0.80m / 1_000_000;
    private const decimal CostPerOutputToken = 4.00m / 1_000_000;

    private readonly AppDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    public EditModel(AppDbContext dbContext, IHttpClientFactory httpClientFactory)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
    }

    public int StudentId { get; private set; }

    [BindProperty] public string FullName { get; set; } = string.Empty;
    [BindProperty] public string? Nickname { get; set; }
    [BindProperty] public Gender Gender { get; set; }
    [BindProperty] public string? Interests { get; set; }
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
    [BindProperty] public string? StudentUsername { get; set; }
    [BindProperty] public string? StudentPin { get; set; }
    public bool HasStudentAccess { get; private set; }
    public bool ShowCredentialInfo { get; private set; }
    public bool JustSaved { get; private set; }

    public async Task<IActionResult> OnGetAsync(int studentId)
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return RedirectToPage("/Login");

        var student = await GetStudentAsync(studentId, familyId.Value);
        if (student is null) return RedirectToPage("/Dashboard");

        LoadFromStudent(student);
        ShowCredentialInfo = Request.Query["credenciales"] == "ok";
        JustSaved = Request.Query["guardado"] == "ok";
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
        student.Interests = string.IsNullOrWhiteSpace(Interests) ? null : Interests.Trim();
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

        // Acceso del alumno
        if (!string.IsNullOrWhiteSpace(StudentUsername))
        {
            var slug = StudentUsername.Trim().ToLowerInvariant();
            var taken = await _dbContext.Users.AnyAsync(u =>
                u.StudentUsername == slug && u.Id != student.Id);
            if (taken)
            {
                ModelState.AddModelError(nameof(StudentUsername), "Ese usuario ya está en uso. Elegí otro.");
                LoadFromStudent(student);
                return Page();
            }
            student.StudentUsername = slug;
        }

        if (!string.IsNullOrWhiteSpace(StudentPin))
        {
            if (StudentPin.Length < 4)
            {
                ModelState.AddModelError(nameof(StudentPin), "El PIN debe tener al menos 4 dígitos.");
                LoadFromStudent(student);
                return Page();
            }
            student.PasswordHash = new PasswordHasher<User>().HashPassword(student, StudentPin);
        }

        var credencialesActualizadas = !string.IsNullOrWhiteSpace(StudentUsername) ||
                                      !string.IsNullOrWhiteSpace(StudentPin);

        try
        {
            await _dbContext.SaveChangesAsync();
            return RedirectToPage("/Students/Edit",
                new { studentId, guardado = "ok", credenciales = credencialesActualizadas ? "ok" : null });
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

        // Hechos de configuración → comportamientos (nunca la etiqueta diagnóstica)
        var facts = new List<string>();
        var quien = student.Gender switch
        {
            Gender.Femenino  => "es una chica",
            Gender.Masculino => "es un chico",
            _                => "es estudiante"
        };
        facts.Add($"{name} {quien} de {student.SchoolLevel} {student.Grade}° año.");
        if (!string.IsNullOrWhiteSpace(student.Interests))
            facts.Add($"Le interesa: {student.Interests}. Lo uso de a ratos para conectar y que se sienta cómodo.");
        if (student.PrefShortMessages)  facts.Add("Le hablo con mensajes muy cortos, un concepto por vez.");
        if (student.PrefVisualExamples) facts.Add("Antes de algo abstracto le doy un ejemplo concreto del mundo real.");
        if (student.PrefSlowPace)       facts.Add("No avanzo al siguiente paso hasta que confirma que entendió.");
        if (student.PrefFrequentPraise) facts.Add("Le reconozco cada avance, no solo el resultado final.");
        if (student.PrefExtraPatience)  facts.Add("Si se frustra, cambio el enfoque en vez de repetir lo mismo.");
        if (student.HasAdhd)
        {
            facts.Add("Necesita paciencia extra y ayuda para sostener el foco; celebro cada micro-logro.");
            if (student.PrefOneQuestionOnly) facts.Add("Nunca le hago más de una pregunta por vez, para no abrumar.");
            if (student.PrefRefocusReminder) facts.Add("Si se desvía del tema, lo traigo de vuelta con calma.");
            facts.Add(student.ExplanationLevel switch
            {
                ExplanationLevel.UnPocoBasico   => "Uso explicaciones un poco más básicas que las de su año.",
                ExplanationLevel.BastanteBasico => "Construyo desde lo más elemental, paso a paso.",
                _                               => "Uso explicaciones acordes a su año."
            });
        }
        facts.Add("Nunca le doy la respuesta directa: lo guío con preguntas hasta que llega solo, le pido que lo intente y no se la resuelvo por cansancio.");

        const string system = """
            Sos el tutor de miTutorIA y le escribís un mensaje breve y cálido al padre o madre de un estudiante,
            para que se quede tranquilo sobre cómo vas a acompañar a su hijo/a en el aula.
            Escribí en español rioplatense, en primera persona ("voy a acompañar", "lo/la voy a..."), de 2 a 3 frases.
            Tono cercano y humano, como un docente de confianza que tranquiliza — no como un sistema.
            NO uses listas, viñetas, títulos, negritas ni emojis. NO menciones diagnósticos ni etiquetas médicas.
            Que NO suene a alerta ni a términos y condiciones. Cerrá transmitiendo tranquilidad.
            """;
        var userMsg = "Configuración de este estudiante:\n- " + string.Join("\n- ", facts);

        string explanation;
        try
        {
            var (reply, tokensIn, tokensOut) = await CallClaudeAsync(system, userMsg, maxTokens: 320);
            explanation = reply.Trim();
            _dbContext.TokenEvents.Add(new TokenEvent
            {
                FamilyId  = familyId.Value,
                UserId    = student.Id,
                TokensIn  = tokensIn,
                TokensOut = tokensOut,
                ModelUsed = ClaudeModel,
                Feature   = "explain",
                CostUsd   = tokensIn * CostPerInputToken + tokensOut * CostPerOutputToken
            });
            await _dbContext.SaveChangesAsync();
        }
        catch
        {
            // Nunca rompemos la página por esto: mensaje cálido de reserva.
            explanation = $"Voy a acompañar a {name} con paciencia y preguntas que lo guíen a pensar, " +
                          "sin darle nunca la respuesta servida. La idea es que se vaya con la sensación de haberlo logrado por su cuenta.";
        }

        return new JsonResult(new { explanation });
    }

    private async Task<(string reply, int tokensIn, int tokensOut)> CallClaudeAsync(
        string systemPrompt, string userMessage, int maxTokens = 320)
    {
        var body = JsonSerializer.Serialize(new
        {
            model      = ClaudeModel,
            max_tokens = maxTokens,
            system     = systemPrompt,
            messages   = new[] { new { role = "user", content = userMessage } }
        });

        var client = _httpClientFactory.CreateClient("anthropic");
        var response = await client.PostAsync("/v1/messages",
            new StringContent(body, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        var reply = root.GetProperty("content")[0].GetProperty("text").GetString() ?? string.Empty;
        var tokensIn  = root.GetProperty("usage").GetProperty("input_tokens").GetInt32();
        var tokensOut = root.GetProperty("usage").GetProperty("output_tokens").GetInt32();
        return (reply, tokensIn, tokensOut);
    }

    private void LoadFromStudent(User student)
    {
        StudentId = student.Id;
        FullName = student.FullName;
        Nickname = student.Nickname;
        Gender = student.Gender;
        Interests = student.Interests;
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
        StudentUsername = student.StudentUsername;
        HasStudentAccess = !string.IsNullOrEmpty(student.StudentUsername) &&
                           !string.IsNullOrEmpty(student.PasswordHash);
    }

    private async Task<User?> GetStudentAsync(int studentId, int familyId) =>
        await _dbContext.Users.SingleOrDefaultAsync(u =>
            u.Id == studentId && u.FamilyId == familyId && u.Role == UserRole.Student);
}
