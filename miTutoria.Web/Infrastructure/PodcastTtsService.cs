using System.Text;
using System.Text.Json;

namespace miTutoria.Web.Infrastructure;

// Un turno del diálogo: quién habla y qué dice.
public sealed record PodcastTurn(string Speaker, string Text);

// Resultado del TTS: el WAV listo para guardar, el modelo usado y los caracteres
// sintetizados (para estimar el costo).
public sealed record PodcastAudio(byte[] Wav, string Mime, string TtsModel, int Chars, int DurationSec);

// Genera el audio multi-speaker con Gemini (una sola llamada). El guión (los turnos)
// lo arma Claude en la página; este servicio solo convierte texto → voz.
public sealed class PodcastTtsService
{
    private const int SampleRate = 24000; // Gemini TTS: PCM 24 kHz, 16-bit, mono

    private readonly IHttpClientFactory _httpFactory;
    private readonly string? _apiKey;

    public PodcastTtsService(IHttpClientFactory httpFactory, IConfiguration config)
    {
        _httpFactory = httpFactory;
        _apiKey = config["GEMINI_API_KEY"];
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    // hostA/hostB: nombres de los dos hosts (deben coincidir con Speaker de los turnos).
    // voiceA/voiceB: voces prebuilt de Gemini (ej. "Kore", "Puck").
    public async Task<PodcastAudio> SynthesizeAsync(
        IReadOnlyList<PodcastTurn> turns,
        string hostA, string voiceA,
        string hostB, string voiceB,
        CancellationToken ct = default)
    {
        if (!IsConfigured) throw new InvalidOperationException("GEMINI_API_KEY no configurada.");

        var client = _httpFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(3);

        var ttsModel = await DiscoverTtsModelAsync(client, ct);

        var sb = new StringBuilder("Leé esta conversación en español, con energía natural y tono amable:\n\n");
        foreach (var t in turns) sb.AppendLine($"{t.Speaker}: {t.Text}");

        var body = new
        {
            contents = new[] { new { parts = new[] { new { text = sb.ToString() } } } },
            generationConfig = new
            {
                responseModalities = new[] { "AUDIO" },
                speechConfig = new
                {
                    multiSpeakerVoiceConfig = new
                    {
                        speakerVoiceConfigs = new[]
                        {
                            new { speaker = hostA, voiceConfig = new { prebuiltVoiceConfig = new { voiceName = voiceA } } },
                            new { speaker = hostB, voiceConfig = new { prebuiltVoiceConfig = new { voiceName = voiceB } } }
                        }
                    }
                }
            }
        };

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{ttsModel}:generateContent?key={_apiKey}";
        using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(url, content, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Gemini TTS {(int)resp.StatusCode}: {raw}");

        using var doc = JsonDocument.Parse(raw);
        var b64 = doc.RootElement.GetProperty("candidates")[0]
            .GetProperty("content").GetProperty("parts")[0]
            .GetProperty("inlineData").GetProperty("data").GetString()
            ?? throw new InvalidOperationException("Gemini TTS no devolvió audio.");

        var pcm = Convert.FromBase64String(b64);
        var wav = WrapWav(pcm, SampleRate);
        var chars = turns.Sum(t => t.Text.Length);
        var durationSec = pcm.Length / (SampleRate * 2); // 16-bit mono
        return new PodcastAudio(wav, "audio/wav", ttsModel, chars, durationSec);
    }

    // Los nombres de modelo de Gemini cambian seguido; preguntamos a la API cuál hay.
    private async Task<string> DiscoverTtsModelAsync(HttpClient client, CancellationToken ct)
    {
        var resp = await client.GetAsync(
            $"https://generativelanguage.googleapis.com/v1beta/models?key={_apiKey}", ct);
        var txt = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Gemini ListModels {(int)resp.StatusCode}: {txt}");

        using var doc = JsonDocument.Parse(txt);
        var names = new List<string>();
        foreach (var m in doc.RootElement.GetProperty("models").EnumerateArray())
        {
            var methods = m.TryGetProperty("supportedGenerationMethods", out var sm)
                ? sm.EnumerateArray().Select(x => x.GetString()).ToHashSet()
                : new HashSet<string?>();
            if (methods.Contains("generateContent"))
                names.Add(m.GetProperty("name").GetString()!.Replace("models/", ""));
        }

        var tts = names
            .Where(n => n.Contains("tts", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(n => n.Contains("flash")) // preferimos flash (más barato)
            .ThenByDescending(n => n)                     // versión más alta
            .FirstOrDefault();

        return tts ?? throw new InvalidOperationException("Gemini no ofrece ningún modelo TTS en esta cuenta.");
    }

    private static byte[] WrapWav(byte[] pcm, int rate)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        int byteRate = rate * 2; // mono, 16-bit
        w.Write(Encoding.ASCII.GetBytes("RIFF"));
        w.Write(36 + pcm.Length);
        w.Write(Encoding.ASCII.GetBytes("WAVE"));
        w.Write(Encoding.ASCII.GetBytes("fmt "));
        w.Write(16); w.Write((short)1); w.Write((short)1);
        w.Write(rate); w.Write(byteRate); w.Write((short)2); w.Write((short)16);
        w.Write(Encoding.ASCII.GetBytes("data"));
        w.Write(pcm.Length);
        w.Write(pcm);
        return ms.ToArray();
    }
}
