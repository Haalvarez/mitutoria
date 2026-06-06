using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace miTutoria.Web.Infrastructure;

/// <summary>
/// Crea preferences de MercadoPago Checkout Pro y consulta pagos.
/// El init_point que devuelve es la página de pago (QR + tarjeta + transferencia):
/// el botón del dashboard linkea ahí y el mail manda la misma URL.
/// Gateado por env MP_ENABLED; sin MP_ACCESS_TOKEN no hace nada.
/// </summary>
public class MercadoPagoService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<MercadoPagoService> _logger;

    public MercadoPagoService(IHttpClientFactory httpClientFactory, IConfiguration config,
        ILogger<MercadoPagoService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public bool IsEnabled =>
        _config.GetValue("MP_ENABLED", false) &&
        !string.IsNullOrWhiteSpace(_config["MP_ACCESS_TOKEN"]);

    public decimal CuotaArs => _config.GetValue<decimal>("CUOTA_ARS", 50000m);

    private HttpClient Client()
    {
        var client = _httpClientFactory.CreateClient("mercadopago");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _config["MP_ACCESS_TOKEN"]);
        return client;
    }

    public record PreferenceResult(string PreferenceId, string InitPoint);

    /// <summary>
    /// Crea una preference de pago para la familia. externalReference enlaza el webhook
    /// con la familia + ciclo: "familyId:cycleMarker".
    /// </summary>
    public async Task<PreferenceResult?> CreatePreferenceAsync(
        int familyId, string email, decimal amountArs, string cycleMarker, string baseUrl, CancellationToken ct = default)
    {
        if (!IsEnabled) return null;

        var body = new
        {
            items = new[]
            {
                new
                {
                    title = "miTutorIA — suscripción mensual",
                    quantity = 1,
                    unit_price = amountArs,
                    currency_id = "ARS"
                }
            },
            payer = new { email },
            external_reference = $"{familyId}:{cycleMarker}",
            back_urls = new
            {
                success = $"{baseUrl}/Dashboard?pago=ok",
                failure = $"{baseUrl}/Dashboard?pago=error",
                pending = $"{baseUrl}/Dashboard?pago=pendiente"
            },
            auto_return = "approved",
            notification_url = $"{baseUrl}/api/pay/webhook"
        };

        try
        {
            var json = JsonSerializer.Serialize(body);
            var resp = await Client().PostAsync("/checkout/preferences",
                new StringContent(json, Encoding.UTF8, "application/json"), ct);
            resp.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            var root = doc.RootElement;
            var id = root.GetProperty("id").GetString() ?? "";
            // init_point = prod; sandbox_init_point = sandbox (según el token usado)
            var initPoint = root.TryGetProperty("init_point", out var ip) ? ip.GetString() : null;
            if (string.IsNullOrEmpty(initPoint) &&
                root.TryGetProperty("sandbox_init_point", out var sip))
                initPoint = sip.GetString();

            if (string.IsNullOrEmpty(initPoint)) return null;
            return new PreferenceResult(id, initPoint);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MercadoPago: no se pudo crear la preference para familia {FamilyId}", familyId);
            return null;
        }
    }

    public record PaymentInfo(string Id, string Status, string? ExternalReference, decimal Amount);

    /// <summary>Consulta un pago por id (lo que llega en el webhook) para confirmar el estado real.</summary>
    public async Task<PaymentInfo?> GetPaymentAsync(string paymentId, CancellationToken ct = default)
    {
        if (!IsEnabled) return null;
        try
        {
            var resp = await Client().GetAsync($"/v1/payments/{paymentId}", ct);
            resp.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            var root = doc.RootElement;
            var id = root.GetProperty("id").GetRawText().Trim('"');
            var status = root.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "";
            var extRef = root.TryGetProperty("external_reference", out var er) ? er.GetString() : null;
            var amount = root.TryGetProperty("transaction_amount", out var ta) && ta.ValueKind == JsonValueKind.Number
                ? ta.GetDecimal() : 0m;
            return new PaymentInfo(id, status, extRef, amount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MercadoPago: no se pudo consultar el pago {PaymentId}", paymentId);
            return null;
        }
    }
}
