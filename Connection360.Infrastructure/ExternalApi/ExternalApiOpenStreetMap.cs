using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Connection360.Infrastructure.ExternalApi.DTOs;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Connection360.Infrastructure.ExternalApi
{
    public class ExternalApiOpenStreetMap : IExternalApiOpenStreetMap
    {
        private readonly ExternalApiSettings _settings;
        private readonly ILogger<ExternalDataApiGateway> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public ExternalApiOpenStreetMap(IHttpClientFactory httpClientFactory, Microsoft.Extensions.Options.IOptions<ExternalApiSettings> settings, ILogger<ExternalDataApiGateway> logger)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<OpenStreetMapDto> GetCoordinates(String apiName, String PlaceName, CancellationToken cancellationToken)
        {
            // Filtrar y obtener la configuración según el nombre enviado
            if (!_settings.Apis.TryGetValue(apiName, out var apiConfig))
            {
                throw new ArgumentException($"La API '{apiName}' no existe en el appsettings.");
            }

            var url = apiConfig.DataEndpoint + $"{Uri.EscapeDataString(PlaceName)}&format=json&limit=1";

            HttpClient httpClient = _httpClientFactory.CreateClient(apiName);

            // Requisito de OpenStreetMap: Debes incluir un User-Agent claro en la cabecera
            httpClient.DefaultRequestHeaders.Add("User-Agent", "MiAppAngularNet10/1.0 (contacto@tuempresa.com)");

            try
            {
                //var response = await client.GetAsync(url);
                using var response = await httpClient.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Error al consultar el servicio de mapas del API externo. Status: {Status}, Body: {Body}", response.StatusCode, body);
                    throw new HttpRequestException($"El API externo respondió con status {(Int32)response.StatusCode}");
                }

                String jsonString = await response.Content.ReadAsStringAsync();
                List<ExternalApiOpenStreetMapResponse>? resultados = JsonSerializer.Deserialize<List<ExternalApiOpenStreetMapResponse>>(jsonString);

                if (resultados == null || resultados.Count == 0)
                {
                    _logger.LogError($"No se encontraron coordenadas para: '{PlaceName}'");
                    throw new HttpRequestException($"No se encontraron coordenadas para: '{PlaceName}'");
                }

                // Tomamos el primer resultado devuelto
                var lugar = resultados[0];

                // Convertimos los strings a double para que Angular los reciba de forma nativa
                OpenStreetMapDto coordinatesResult = new OpenStreetMapDto
                {
                    PlaceName = lugar.DisplayName,
                    Latitud = lugar.Lat,
                    Longitud = lugar.Lon
                };

                return coordinatesResult;
            }
            catch (Exception ex)
            {
                _logger.LogError($" Error {ex} ");
                throw;
            }
        }
    }
}
