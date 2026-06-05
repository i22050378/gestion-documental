using System.Net.Http.Json;

namespace Central.Services;

// Cliente que habla con el modulo de Indexacion (Node) por HTTP.
// Si el servicio Node esta caido, NO rompe la aprobacion: solo devuelve false.
public class IndexacionClient
{
    private readonly HttpClient _http;
    private readonly ILogger<IndexacionClient> _logger;

    public IndexacionClient(HttpClient http, ILogger<IndexacionClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    // Envia (o actualiza) el metadato de una version aprobada.
    public async Task<bool> IndexarAsync(object metadato)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("/api/metadatos", metadato);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo indexar el metadato en el modulo de Indexacion (Node).");
            return false;
        }
    }
}
