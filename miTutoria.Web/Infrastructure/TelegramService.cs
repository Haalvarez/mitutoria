namespace miTutoria.Web.Infrastructure;

public class TelegramService
{
    private readonly IHttpClientFactory _http;
    private readonly string? _botToken;
    private readonly string? _chatId;

    public TelegramService(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _botToken = config["TELEGRAM_BOT_TOKEN"];
        _chatId   = config["TELEGRAM_CHAT_ID"];
    }

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

        try { await client.PostAsync(url, body); }
        catch { /* no romper el flujo si Telegram falla */ }
    }
}
