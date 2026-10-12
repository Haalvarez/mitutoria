using System.Text;
using System.Text.Json;

namespace miTutoria.Web.Infrastructure;

/// <summary>
/// Modelo, precios y lectura de respuesta compartidos por todas las llamadas a Claude.
/// </summary>
public static class ClaudeApi
{
    public const string Model = "claude-haiku-5-5";

    // USD por token (prompts ≤100K tokens; los nuestros están muy por debajo).
    public const decimal CostPerInputToken  = 0.10m / 1_000_000;
    public const decimal CostPerOutputToken = 0.50m / 1_000_000;

    // Chat de materias de cálculo (modo Resolución: mate, física, química...): acá un error del
    // tutor no tiene vuelta atrás, así que va Sonnet 5.5 con pensamiento adaptativo.
    public const string MathModel = "claude-sonnet-5-5";
    public const string MathEffort = "medium";
    public const int MathMaxTokens = 8000;   // incluye el pensamiento, no solo la respuesta visible
    public const string FallbackBeta = "server-side-fallback-2026-07-01";

    // Mira el modo y también el nombre: las aulas creadas antes de que existiera el modo quedaron
    // en Comprensión por default aunque sean de Matemática.
    public static bool UsesMathModel(Data.Entities.Academic.Classroom classroom)
        => classroom.Mode == Data.Entities.Academic.PedagogicalMode.Resolucion
           || CalcKeywords.Any(k => classroom.Name.ToLowerInvariant().Contains(k));

    private static readonly string[] CalcKeywords =
    {
        "matem", "mate", "álgebra", "algebra", "geometr", "trigonometr",
        "física", "fisica", "químic", "quimic", "cálculo", "calculo",
        "contab", "estadística", "estadistica", "aritmét", "aritmet"
    };

    /// <summary>Costo en USD según el modelo que efectivamente respondió (campo "model" de la respuesta).</summary>
    public static decimal Cost(string model, int tokensIn, int tokensOut)
    {
        var (inUsd, outUsd) = model switch
        {
            var m when m.StartsWith("claude-haiku-5")  => (0.10m, 0.50m),
            var m when m.StartsWith("claude-sonnet-5") => (2.00m, 10.00m),
            var m when m.StartsWith("claude-haiku-4")  => (1.00m, 5.00m),
            _                                          => (4.00m, 20.00m)   // fallback a Opus u otro: conservador
        };
        return tokensIn * inUsd / 1_000_000 + tokensOut * outUsd / 1_000_000;
    }

    // Haiku 5.5 piensa por defecto: lo apagamos para mantener la latencia y el comportamiento de antes.
    public static readonly object NoThinking = new { type = "disabled" };

    // El tokenizer de Haiku 5.5 cuenta ~30% más tokens para el mismo texto: escalamos los topes
    // pensados para Haiku 4.5 para no cortar respuestas.
    public static int MaxTokens(int haiku45Budget) => (int)Math.Ceiling(haiku45Budget * 1.3);

    /// <summary>
    /// Concatena los bloques de texto de la respuesta (no asume que el primero es texto).
    /// Si el modelo declinó el pedido (stop_reason "refusal") y no hay texto, devuelve <paramref name="refusalFallback"/>.
    /// </summary>
    public static string ExtractText(JsonElement root, string refusalFallback = "")
    {
        var sb = new StringBuilder();
        if (root.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
        {
            foreach (var block in content.EnumerateArray())
            {
                if (block.TryGetProperty("type", out var t) && t.GetString() == "text"
                    && block.TryGetProperty("text", out var text))
                    sb.Append(text.GetString());
            }
        }

        if (sb.Length == 0
            && root.TryGetProperty("stop_reason", out var stop) && stop.GetString() == "refusal")
            return refusalFallback;

        return sb.ToString();
    }
}
