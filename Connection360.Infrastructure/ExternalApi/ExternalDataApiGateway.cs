using Connection360.Application.Ports;
using Connection360.Domain.Entities;
using Connection360.Infrastructure.ExternalApi.DTOs;
using Connection360.Infrastructure.Mappers;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Connection360.Infrastructure.ExternalApi
{
    /// <summary>
    /// Clase para consultar datos en la apis externas
    /// </summary>
    public class ExternalDataApiGateway : IExternalDataGateway
    {
        private readonly HttpClient _httpClient;
        private readonly ExternalApiSettings _settings;
        private readonly ILogger<ExternalDataApiGateway> _logger;

        /// <summary>
        /// Constructor de la clase
        /// </summary>
        /// <param name="httpClient">Objeto para hacer la petición</param>
        /// <param name="settings">Configuraciones de la api a consultar</param>
        /// <param name="logger">Objeto para realizar logs de la petición</param>
        public ExternalDataApiGateway(HttpClient httpClient, Microsoft.Extensions.Options.IOptions<ExternalApiSettings> settings, ILogger<ExternalDataApiGateway> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;

            if (!String.IsNullOrWhiteSpace(_settings.ApiKey))
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _settings.ApiKey);
        }

        /// <summary>
        /// Metodo que hace la consulta de datos a la api
        /// </summary>
        /// <param name="filters">Filtros que se necesiten agregar a la api</param>
        /// <param name="cancellationToken">Objeto para controlar la solicutud de la tarea a ejecutar</param>
        /// <returns></returns>
        /// <exception cref="HttpRequestException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<DynamicDataSet> FetchDataAsync(IDictionary<String, String> filters, CancellationToken cancellationToken)
        {
            var query = String.Join("&", filters.Select(f => $"{Uri.EscapeDataString(f.Key)}={Uri.EscapeDataString(f.Value)}"));
            var url = String.IsNullOrEmpty(query)
                ? _settings.DataEndpoint
                : $"{_settings.DataEndpoint}?{query}";

            _logger.LogInformation("Consultando API externo: {Url}", url);

            using var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Error del API externo. Status: {Status}, Body: {Body}", response.StatusCode, body);
                throw new HttpRequestException(
                    $"El API externo respondió con status {(Int32)response.StatusCode}");
            }

            var dto = await response.Content.ReadFromJsonAsync<ExternalApiResponseDto>(
                cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Respuesta vacía del API externo.");

            if (dto is null)
                return new DynamicDataSet(Enumerable.Empty<String>(), Enumerable.Empty<DynamicRecord>());

            DynamicDataSet domainDataSet = dto.ToDomainDataSet();
            return domainDataSet;
        }
    }
}
