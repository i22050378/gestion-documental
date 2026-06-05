using System.Net.Http.Json;

namespace Central.Services;

// Cliente que habla con el modulo de Consulta (PHP) por HTTP.
// Si el portal PHP esta caido, NO rompe la aprobacion: solo devuelve false.
public class ConsultaClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ConsultaClient> _logger;

    public ConsultaClient(HttpClient http, ILogger<ConsultaClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    // Publica (o actualiza) un documento aprobado en el portal de consulta.
    public async Task<bool> PublicarAprobadoAsync(object documento)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("/?r=api/aprobados", documento);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo publicar el documento en el modulo de Consulta (PHP).");
            return false;
        }
    }
}
