namespace miTutoria.Web.Infrastructure;

public class TelegramService
{
    private readonly IHttpClientFactory _http;
    private readonly ILogger<TelegramService> _logger;
    private readonly string? _botToken;
    private readonly string? _chatId;

    public TelegramService(IHttpClientFactory http, IConfiguration config, ILogger<TelegramService> logger)
    {
        _http = http;
        _logger = logger;
        _botToken = config["TELEGRAM_BOT_TOKEN"];
        _chatId   = config["TELEGRAM_CHAT_ID"];
    }

    // Escapa los caracteres reservados de parse_mode=HTML (&, <, >). Sin esto, un valor con
    // '<' rompe el mensaje (Telegram devuelve 400) o permite spoofear su contenido.
    public static string EscapeHtml(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    public async Task SendAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(_botToken) || string.IsNullOrWhiteSpace(_chatId))
            return;

        var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
        var client = _http.CreateClient();
        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["chat_id"] = _chatId,
            ["text"]    = message,
            ["parse_mode"] = "HTML"
        });

        try
        {
            var response = await client.PostAsync(url, body);
            if (!response.IsSuccessStatusCode)
            {
                // Antes se tragaba en silencio: una notificación fallida = un lead que nadie ve.
                var detail = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Telegram sendMessage falló ({Status}): {Detail}", (int)response.StatusCode, detail);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Telegram sendMessage lanzó excepción");
        }
    }
}
