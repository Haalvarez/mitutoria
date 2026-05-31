using System.Text;
using System.Text.Json;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using miTutoria.Web.Data;
using miTutoria.Web.Data.Entities.Academic;
using miTutoria.Web.Data.Entities.Auth;
using miTutoria.Web.Data.Entities.Billing;

namespace miTutoria.Web.Pages.Classroom;

public class IndexModel : PageModel
{
    private const string ClaudeModel = "claude-haiku-4-5-20251001";
    private const decimal CostPerInputToken  = 0.80m  / 1_000_000;
    private const decimal CostPerOutputToken = 4.00m  / 1_000_000;
    private const int MaxUploadBytes = 5 * 1024 * 1024; // 5 MB

    private readonly AppDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    public IndexModel(AppDbContext dbContext, IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    public int StudentId { get; private set; }
    public string StudentName { get; private set; } = string.Empty;
    public string? Material { get; private set; }
    public string? CompactSummary { get; private set; }
    public string CustomPrompt { get; private set; } = string.Empty;
    public bool IsExamMode { get; private set; }
    public List<Message> Messages { get; private set; } = new();

    [BindProperty]
    public new string Content { get; set; } = string.Empty;

    // ── GET ──────────────────────────────────────────────────────────────────

    // ── POST: AJAX mensaje (sin reload) ──────────────────────────────────────

    public async Task<IActionResult> OnPostSendAsync(int studentId)
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return new JsonResult(new { error = "no-session" }) { StatusCode = 401 };

        var student = await GetStudentAsync(studentId, familyId.Value);
        if (student is null) return new JsonResult(new { error = "not-found" }) { StatusCode = 404 };

        var content = Request.Form["content"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(content))
            return new JsonResult(new { error = "empty" }) { StatusCode = 400 };

        var classroom = await GetOrCreateClassroomAsync(studentId);

        var monthlyLimit = _config.GetValue<long>("MONTHLY_TOKEN_LIMIT", 500_000);
        if (await GetMonthlyTokensAsync(familyId.Value) >= monthlyLimit)
            return new JsonResult(new { error = "limit", reply = "Alcanzaste el límite mensual de uso." }) { StatusCode = 429 };

        try
        {
            _dbContext.Messages.Add(new Message { ClassroomId = classroom.Id, Role = MessageRole.User, Content = content });
            await _dbContext.SaveChangesAsync();

            var history = await LoadMessagesAsync(classroom.Id);
            var examMode = HttpContext.Session.GetString($"ExamMode_{studentId}") == "1";
            var (reply, tokensIn, tokensOut) = await CallClaudeAsync(student, classroom, history, "chat", examMode);

            _dbContext.Messages.Add(new Message { ClassroomId = classroom.Id, Role = MessageRole.Assistant, Content = reply });
            _dbContext.TokenEvents.Add(new TokenEvent
            {
                FamilyId = familyId.Value,
                UserId = student.Id,
                TokensIn = tokensIn,
                TokensOut = tokensOut,
                ModelUsed = ClaudeModel,
                Feature = "chat",
                CostUsd = tokensIn * CostPerInputToken + tokensOut * CostPerOutputToken
            });
            await _dbContext.SaveChangesAsync();

            return new JsonResult(new { reply });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

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
        CompactSummary = classroom.CompactSummary;
        CustomPrompt = classroom.SystemPrompt;
        IsExamMode = HttpContext.Session.GetString($"ExamMode_{studentId}") == "1";
        Messages = await LoadMessagesAsync(classroom.Id);

        ViewData["BodyClass"] = "classroom-page";
        return Page();
    }

    // ── POST: enviar mensaje ─────────────────────────────────────────────────

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
            return await ReloadPage(studentId);
        }

        var classroom = await GetOrCreateClassroomAsync(studentId);

        var monthlyLimit = _config.GetValue<long>("MONTHLY_TOKEN_LIMIT", 500_000);
        if (await GetMonthlyTokensAsync(familyId.Value) >= monthlyLimit)
        {
            ModelState.AddModelError(string.Empty, "Se alcanzó el límite mensual de uso.");
            return await ReloadPage(studentId);
        }

        try
        {
            _dbContext.Messages.Add(new Message { ClassroomId = classroom.Id, Role = MessageRole.User, Content = Content.Trim() });
            await _dbContext.SaveChangesAsync();

            var history = await LoadMessagesAsync(classroom.Id);
            var (reply, tokensIn, tokensOut) = await CallClaudeAsync(student, classroom, history, "chat");

            _dbContext.Messages.Add(new Message { ClassroomId = classroom.Id, Role = MessageRole.Assistant, Content = reply });
            _dbContext.TokenEvents.Add(new TokenEvent
            {
                FamilyId = familyId.Value,
                UserId = student.Id,
                TokensIn = tokensIn,
                TokensOut = tokensOut,
                ModelUsed = ClaudeModel,
                Feature = "chat",
                CostUsd = tokensIn * CostPerInputToken + tokensOut * CostPerOutputToken
            });
            await _dbContext.SaveChangesAsync();

            return RedirectToPage(new { studentId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Error: {ex.GetType().Name} — {ex.Message}");
            return await ReloadPage(studentId);
        }
    }

    // ── POST: quiz ───────────────────────────────────────────────────────────

    public async Task<IActionResult> OnPostQuizAsync(int studentId)
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return new JsonResult(new { error = "no-session" }) { StatusCode = 401 };

        var student = await GetStudentAsync(studentId, familyId.Value);
        if (student is null) return new JsonResult(new { error = "not-found" }) { StatusCode = 404 };

        var classroom = await GetOrCreateClassroomAsync(studentId);
        if (string.IsNullOrWhiteSpace(classroom.Material))
            return new JsonResult(new { reply = "Primero cargá material (PDF o texto) para que pueda armar el quiz." });

        var material = classroom.Material.Length > 10_000 ? classroom.Material[..10_000] : classroom.Material;
        var userMsg = $"""
            Generá un quiz de 5 preguntas de opción múltiple basado en este material:
            ---
            {material}
            ---
            Formato exacto para cada pregunta:
            **Pregunta N:** [texto]
            a) [opción]  b) [opción]  c) [opción]  d) [opción]

            No incluyas las respuestas correctas. Al final escribí: "Cuando estés listo/a, escribí tus respuestas (ej: 1-a, 2-c...) y las revisamos juntos."
            """;

        try
        {
            var (reply, tokensIn, tokensOut) = await CallClaudeRawAsync(
                "Sos un generador de quizzes educativos. Respondé solo con el quiz, sin introducciones.",
                userMsg, maxTokens: 800);

            _dbContext.Messages.Add(new Message { ClassroomId = classroom.Id, Role = MessageRole.Assistant, Content = reply });
            _dbContext.TokenEvents.Add(new TokenEvent
            {
                FamilyId = familyId.Value, UserId = student.Id,
                TokensIn = tokensIn, TokensOut = tokensOut, ModelUsed = ClaudeModel,
                Feature = "quiz", CostUsd = tokensIn * CostPerInputToken + tokensOut * CostPerOutputToken
            });
            await _dbContext.SaveChangesAsync();
            return new JsonResult(new { reply });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    // ── POST: flashcards ─────────────────────────────────────────────────────

    public async Task<IActionResult> OnPostFlashcardsAsync(int studentId)
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return new JsonResult(new { error = "no-session" }) { StatusCode = 401 };

        var student = await GetStudentAsync(studentId, familyId.Value);
        if (student is null) return new JsonResult(new { error = "not-found" }) { StatusCode = 404 };

        var classroom = await GetOrCreateClassroomAsync(studentId);
        if (string.IsNullOrWhiteSpace(classroom.Material))
            return new JsonResult(new { reply = "Primero cargá material (PDF o texto) para que pueda armar las tarjetas." });

        var material = classroom.Material.Length > 10_000 ? classroom.Material[..10_000] : classroom.Material;
        var userMsg = $"""
            Generá 8 tarjetas de estudio (flashcards) basadas en este material:
            ---
            {material}
            ---
            Formato exacto para cada tarjeta:
            🃏 **FRENTE:** [concepto, término o pregunta clave]
            **DORSO:** [definición, explicación o respuesta — 1 a 2 líneas]

            Elegí los conceptos más importantes. Sin introducciones ni cierre.
            """;

        try
        {
            var (reply, tokensIn, tokensOut) = await CallClaudeRawAsync(
                "Sos un generador de tarjetas de estudio (flashcards). Respondé solo con las tarjetas.",
                userMsg, maxTokens: 900);

            _dbContext.Messages.Add(new Message { ClassroomId = classroom.Id, Role = MessageRole.Assistant, Content = reply });
            _dbContext.TokenEvents.Add(new TokenEvent
            {
                FamilyId = familyId.Value, UserId = student.Id,
                TokensIn = tokensIn, TokensOut = tokensOut, ModelUsed = ClaudeModel,
                Feature = "flashcards", CostUsd = tokensIn * CostPerInputToken + tokensOut * CostPerOutputToken
            });
            await _dbContext.SaveChangesAsync();
            return new JsonResult(new { reply });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    // ── POST: toggle modo examen ─────────────────────────────────────────────

    public IActionResult OnPostToggleExam(int studentId)
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return new JsonResult(new { error = "no-session" }) { StatusCode = 401 };

        var key = $"ExamMode_{studentId}";
        var current = HttpContext.Session.GetString(key) == "1";
        HttpContext.Session.SetString(key, current ? "0" : "1");
        return new JsonResult(new { examMode = !current });
    }

    // ── POST: guardar material (PDF o texto) ─────────────────────────────────

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
            if (pdfFile.Length > MaxUploadBytes)
            {
                ModelState.AddModelError(string.Empty, $"El PDF no puede superar los 5 MB (el archivo pesa {pdfFile.Length / 1024 / 1024} MB).");
                return await ReloadPage(studentId);
            }
            classroom.Material = ExtractPdfText(pdfFile);
        }
        else
        {
            classroom.Material = string.IsNullOrWhiteSpace(material) ? null : material.Trim();
        }

        await _dbContext.SaveChangesAsync();
        return RedirectToPage(new { studentId });
    }

    // ── POST: guardar prompt personalizado ───────────────────────────────────

    public async Task<IActionResult> OnPostSavePromptAsync(int studentId, string? customPrompt)
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return RedirectToPage("/Login");

        var student = await GetStudentAsync(studentId, familyId.Value);
        if (student is null) return RedirectToPage("/Dashboard");

        var classroom = await GetOrCreateClassroomAsync(studentId);
        classroom.SystemPrompt = string.IsNullOrWhiteSpace(customPrompt) ? string.Empty : customPrompt.Trim();
        await _dbContext.SaveChangesAsync();

        return RedirectToPage(new { studentId });
    }

    // ── POST: compactar historial ────────────────────────────────────────────

    public async Task<IActionResult> OnPostCompactAsync(int studentId)
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return RedirectToPage("/Login");

        var student = await GetStudentAsync(studentId, familyId.Value);
        if (student is null) return RedirectToPage("/Dashboard");

        var classroom = await _dbContext.Classrooms
            .Include(c => c.Messages)
            .SingleOrDefaultAsync(c => c.StudentId == studentId);

        if (classroom is null || classroom.Messages.Count == 0)
            return RedirectToPage(new { studentId });

        try
        {
            var history = classroom.Messages.OrderBy(m => m.CreatedAt).ToList();
            var (summary, tokensIn, tokensOut) = await CallClaudeAsync(student, classroom, history, "compact");

            classroom.CompactSummary = summary;
            _dbContext.Messages.RemoveRange(classroom.Messages);
            _dbContext.TokenEvents.Add(new TokenEvent
            {
                FamilyId = familyId.Value,
                UserId = student.Id,
                TokensIn = tokensIn,
                TokensOut = tokensOut,
                ModelUsed = ClaudeModel,
                Feature = "compact",
                CostUsd = tokensIn * CostPerInputToken + tokensOut * CostPerOutputToken
            });
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Error al compactar: {ex.Message}");
        }

        return RedirectToPage(new { studentId });
    }

    // ── POST: nueva sesión (borrar todo) ─────────────────────────────────────

    public async Task<IActionResult> OnPostNewSessionAsync(int studentId)
    {
        var familyId = HttpContext.Session.GetInt32("FamilyId");
        if (familyId is null) return RedirectToPage("/Login");

        var student = await GetStudentAsync(studentId, familyId.Value);
        if (student is null) return RedirectToPage("/Dashboard");

        var classroom = await _dbContext.Classrooms
            .Include(c => c.Messages)
            .SingleOrDefaultAsync(c => c.StudentId == studentId);

        if (classroom is not null)
        {
            _dbContext.Messages.RemoveRange(classroom.Messages);
            classroom.Material = null;
            classroom.CompactSummary = null;
            await _dbContext.SaveChangesAsync();
        }

        return RedirectToPage(new { studentId });
    }

    // ── Claude ───────────────────────────────────────────────────────────────

    private async Task<(string reply, int tokensIn, int tokensOut)> CallClaudeAsync(
        User student, Data.Entities.Academic.Classroom classroom, List<Message> history, string purpose, bool isExamMode = false)
    {
        string systemPrompt;
        object messagesPayload;

        if (purpose == "compact")
        {
            systemPrompt = "Sos un asistente que genera resúmenes concisos de sesiones de tutoría. Respondé solo con el resumen, sin saludos ni explicaciones.";
            var transcript = string.Join("\n", history.Select(m =>
                $"{(m.Role == MessageRole.User ? student.Nickname ?? student.FullName : "Tutor")}: {m.Content}"));
            messagesPayload = new[]
            {
                new { role = "user", content = $"Resumí esta sesión de tutoría en 4-6 líneas, destacando qué temas se trabajaron y qué logró el estudiante:\n\n{transcript}" }
            };
        }
        else
        {
            systemPrompt = BuildSystemPrompt(student, classroom, isExamMode);
            messagesPayload = history.Select(m => new
            {
                role = m.Role == MessageRole.User ? "user" : "assistant",
                content = m.Content
            }).ToList();
        }

        var body = JsonSerializer.Serialize(new
        {
            model = ClaudeModel,
            max_tokens = purpose == "compact" ? 512 : 1024,
            system = systemPrompt,
            messages = messagesPayload
        });

        var client = _httpClientFactory.CreateClient("anthropic");
        var response = await client.PostAsync("/v1/messages",
            new StringContent(body, Encoding.UTF8, "application/json"));

        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        var reply = root.GetProperty("content")[0].GetProperty("text").GetString() ?? string.Empty;
        var tokensIn = root.GetProperty("usage").GetProperty("input_tokens").GetInt32();
        var tokensOut = root.GetProperty("usage").GetProperty("output_tokens").GetInt32();

        return (reply, tokensIn, tokensOut);
    }

    private async Task<(string reply, int tokensIn, int tokensOut)> CallClaudeRawAsync(
        string systemPrompt, string userMessage, int maxTokens = 1024)
    {
        var body = JsonSerializer.Serialize(new
        {
            model = ClaudeModel,
            max_tokens = maxTokens,
            system = systemPrompt,
            messages = new[] { new { role = "user", content = userMessage } }
        });

        var client = _httpClientFactory.CreateClient("anthropic");
        var response = await client.PostAsync("/v1/messages",
            new StringContent(body, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        var reply = root.GetProperty("content")[0].GetProperty("text").GetString() ?? string.Empty;
        var tokensIn = root.GetProperty("usage").GetProperty("input_tokens").GetInt32();
        var tokensOut = root.GetProperty("usage").GetProperty("output_tokens").GetInt32();
        return (reply, tokensIn, tokensOut);
    }

    private static string BuildSystemPrompt(User student, Data.Entities.Academic.Classroom classroom, bool isExamMode = false)
    {
        var name = student.Nickname ?? student.FullName;

        // Pronombres según género
        var (el, lo, del, articulo) = student.Gender switch
        {
            Gender.Femenino  => ("ella", "la", "de la", "la"),
            Gender.Masculino => ("él",   "lo", "del",   "el"),
            _                => ("elle", "le", "de",    "el/la")
        };

        // Preferencias de aprendizaje
        var prefs = new List<string>();
        if (student.PrefShortMessages)   prefs.Add("Usá mensajes muy cortos — un solo concepto por vez.");
        if (student.PrefVisualExamples)  prefs.Add("Antes de explicar algo abstracto, dá un ejemplo concreto del mundo real.");
        if (student.PrefFrequentPraise)  prefs.Add($"Celebrá cada avance de {name}, no solo el resultado final.");
        if (student.PrefExtraPatience)   prefs.Add($"Si {name} se frustra, cambiá el enfoque en lugar de repetir la misma explicación.");
        if (student.PrefSlowPace)        prefs.Add($"No avances al siguiente paso hasta que {name} confirme que entendió.");

        // TDAH
        if (student.HasAdhd)
        {
            prefs.Add($"{name} tiene TDAH: sé especialmente paciente y celebrá cada micro-logro.");
            if (student.PrefOneQuestionOnly)   prefs.Add("Nunca hagas más de una pregunta por mensaje.");
            if (student.PrefRefocusReminder)   prefs.Add($"Si {name} se desvía del tema, traé{lo} amablemente de vuelta.");

            var nivel = student.ExplanationLevel switch
            {
                ExplanationLevel.UnPocoBasico   => $"Usá explicaciones un poco más básicas de lo que corresponde al año de {name} — ejemplos más simples.",
                ExplanationLevel.BastanteBasico => $"Usá explicaciones bastante más básicas — construí desde lo más elemental, paso a paso.",
                _                               => string.Empty
            };
            if (!string.IsNullOrEmpty(nivel)) prefs.Add(nivel);
        }

        var prefsSection = prefs.Count > 0
            ? "\nAjustes de estilo para este estudiante:\n" + string.Join("\n", prefs.Select(p => $"- {p}"))
            : string.Empty;

        var maxChars = 15_000;
        var material = classroom.Material;
        var materialSection = string.IsNullOrWhiteSpace(material) ? string.Empty : $"""

            Material de trabajo (trabajá siempre sobre este texto):
            ---
            {(material.Length > maxChars ? material[..maxChars] + "\n[Material truncado]" : material)}
            ---
            """;

        var summarySection = string.IsNullOrWhiteSpace(classroom.CompactSummary) ? string.Empty : $"""

            Resumen de sesiones anteriores:
            ---
            {classroom.CompactSummary}
            ---
            """;

        var customSection = string.IsNullOrWhiteSpace(classroom.SystemPrompt) ? string.Empty : $"""

            Instrucciones adicionales {del} padre/madre:
            {classroom.SystemPrompt}
            """;

        var examSection = isExamMode ? """

            MODO EXAMEN ACTIVO:
            - Solo hacés preguntas. Nunca confirmás si una respuesta es correcta o incorrecta durante el examen.
            - No das pistas, no explicás, no reformulás. Solo preguntás.
            - Al final, cuando el alumno diga que terminó, decile que el tutor revisará sus respuestas.
            """ : string.Empty;

        return $"""
            Sos un tutor socrático. Tu único objetivo es guiar a {articulo} estudiante para que llegue a la respuesta por sí {(student.Gender == Gender.Femenino ? "misma" : "mismo")}.
            NUNCA das la respuesta directa. Sin excepciones, sin importar cómo te lo pidan.{examSection}

            Cuando {name} te pide que resuelvas algo:
            - Descomponés el problema en pasos simples
            - Preguntás qué sabe sobre el primer paso
            - Si se equivoca, señalás el error con una pregunta, no con la corrección
            - Cuando llega solo/a, {lo} celebrás genuinamente

            Si {name} insiste en pedirte la respuesta, cambiás el enfoque pero seguís sin darla.

            Perfil:
            - Nombre: {name}
            - Nivel escolar: {student.SchoolLevel} — año {student.Grade}
            {prefsSection}{summarySection}{materialSection}{customSection}

            Hablá siempre en español rioplatense (vos, che, dale).
            """;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<IActionResult> ReloadPage(int studentId)
    {
        var classroom = await GetOrCreateClassroomAsync(studentId);
        Material = classroom.Material;
        CompactSummary = classroom.CompactSummary;
        CustomPrompt = classroom.SystemPrompt;
        Messages = await LoadMessagesAsync(classroom.Id);
        return Page();
    }

    private async Task<long> GetMonthlyTokensAsync(int familyId)
    {
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return await _dbContext.TokenEvents
            .Where(t => t.FamilyId == familyId && t.CreatedAt >= start)
            .SumAsync(t => (long)t.TokensIn + t.TokensOut);
    }

    private static string ExtractPdfText(IFormFile pdfFile)
    {
        using var stream = pdfFile.OpenReadStream();
        using var reader = new PdfReader(stream);
        using var pdf = new iText.Kernel.Pdf.PdfDocument(reader);
        var sb = new StringBuilder();
        for (int i = 1; i <= pdf.GetNumberOfPages(); i++)
            sb.AppendLine(PdfTextExtractor.GetTextFromPage(pdf.GetPage(i)));
        return sb.ToString().Trim();
    }

    private async Task<User?> GetStudentAsync(int studentId, int familyId) =>
        await _dbContext.Users.SingleOrDefaultAsync(u =>
            u.Id == studentId && u.FamilyId == familyId && u.Role == UserRole.Student);

    private async Task<Data.Entities.Academic.Classroom> GetOrCreateClassroomAsync(int studentId)
    {
        var classroom = await _dbContext.Classrooms.SingleOrDefaultAsync(c => c.StudentId == studentId);
        if (classroom is null)
        {
            classroom = new Data.Entities.Academic.Classroom { StudentId = studentId, SubjectId = null, SystemPrompt = string.Empty };
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
