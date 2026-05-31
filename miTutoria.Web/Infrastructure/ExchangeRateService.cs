using System.Text.Json;

namespace miTutoria.Web.Infrastructure;

public class ExchangeRateService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExchangeRateService> _logger;

    private decimal _cachedRate = 1400m;
    private DateTime _cachedAt = DateTime.MinValue;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private const int CacheMinutes = 60;
    private const decimal FallbackRate = 1400m;

    public ExchangeRateService(IHttpClientFactory httpClientFactory, ILogger<ExchangeRateService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<decimal> GetMepRateAsync()
    {
        if (DateTime.UtcNow - _cachedAt < TimeSpan.FromMinutes(CacheMinutes))
            return _cachedRate;

        await _lock.WaitAsync();
        try
        {
            if (DateTime.UtcNow - _cachedAt < TimeSpan.FromMinutes(CacheMinutes))
                return _cachedRate;

            var client = _httpClientFactory.CreateClient("dolarapi");
            var response = await client.GetAsync("/v1/dolares/bolsa");
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var venta = doc.RootElement.GetProperty("venta").GetDecimal();

            _cachedRate = venta > 0 ? venta : FallbackRate;
            _cachedAt = DateTime.UtcNow;

            _logger.LogInformation("Tipo de cambio MEP actualizado: {Rate} ARS/USD", _cachedRate);
            return _cachedRate;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo obtener el tipo de cambio MEP, usando fallback {Rate}", _cachedRate);
            return _cachedRate;
        }
        finally
        {
            _lock.Release();
        }
    }
}
