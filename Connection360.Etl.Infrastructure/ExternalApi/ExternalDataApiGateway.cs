using Connection360.Etl.Application.Ports;
using Connection360.Etl.Domain.Entities;
using Connection360.Etl.Infrastructure.ExternalApi.DTOs;
using Connection360.Etl.Infrastructure.Mappers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Connection360.Etl.Infrastructure.ExternalApi
{
    /// <summary>
    /// Adaptador que consulta las APIs externas configuradas en la sección "ExternalApi" del
    /// appsettings. Copiado de Connection360.Infrastructure.ExternalApi.ExternalDataApiGateway
    /// (misma validación de configuración, armado de query string con los filtros recibidos, y uso
    /// del <see cref="IHttpClientFactory"/> con el cliente nombrado registrado para esa API),
    /// extendido con el modo paginado: la petición HTTP en sí (<see cref="FetchInternalAsync"/>) es
    /// exactamente la misma para ambos modos, solo cambia qué filtros se le agregan antes de llamarla.
    /// </summary>
    public class ExternalDataApiGateway : IExternalDataGateway
    {
        private readonly ExternalApiSettings _settings;
        private readonly ILogger<ExternalDataApiGateway> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public ExternalDataApiGateway(IHttpClientFactory httpClientFactory, IOptions<ExternalApiSettings> settings, ILogger<ExternalDataApiGateway> logger)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
            _logger = logger;
        }

        /// <exception cref="ArgumentException">La API solicitada no existe en la configuración.</exception>
        /// <exception cref="HttpRequestException">La API externa respondió con un status de error.</exception>
        /// <exception cref="InvalidOperationException">La respuesta del API externo llegó vacía.</exception>
        public async IAsyncEnumerable<DynamicDataSet> FetchDataPagedAsync(
            String apiName,
            IDictionary<String, String> filters,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (!_settings.Apis.TryGetValue(apiName, out var apiConfig))
            {
                throw new ArgumentException($"La API '{apiName}' no existe en el appsettings.");
            }

            // El tamaño de página es GLOBAL (ExternalApiSettings.PageSize), no por API: así todas las
            // APIs paginan en la misma "unidad", lo que garantiza que la página N de una corresponda a
            // la misma porción de datos que la página N de otra al combinarlas (merge) por ronda en
            // RunEtlProcessUseCase.
            Boolean apiPagina = _settings.PaginationEnabled && _settings.PageSize > 0;

            if (!apiPagina)
            {
                // Modo actual (sin paginación): una sola consulta con el 100% de los datos.
                yield return await FetchInternalAsync(apiName, filters, cancellationToken);
                yield break;
            }

            Int32 pageNumber = 1;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Copia de los filtros del caller + los parámetros de paginación de esta página:
                // nunca se muta el diccionario recibido, ya que se reutiliza en cada iteración.
                var pageFilters = new Dictionary<String, String>(filters, StringComparer.OrdinalIgnoreCase)
                {
                    [apiConfig.PageNumberParam] = pageNumber.ToString(CultureInfo.InvariantCulture),
                    [apiConfig.PageSizeParam] = _settings.PageSize.ToString(CultureInfo.InvariantCulture)
                };

                DynamicDataSet page = await FetchInternalAsync(apiName, pageFilters, cancellationToken);

                _logger.LogInformation("Página {Page} de {Api}: {Count} registros.", pageNumber, apiName, page.Rows.Count);

                if (page.Rows.Count == 0)
                    yield break; // Página vacía: no hay más datos que traer.

                yield return page;

                if (page.Rows.Count < _settings.PageSize)
                    yield break; // Página incompleta: era la última, no hace falta pedir la siguiente.

                pageNumber++;
            }
        }

        /// <summary>
        /// Ejecuta la petición HTTP en sí (misma lógica que antes tenía el único método que existía
        /// para consultar la API completa): arma la query string con <paramref name="filters"/> (que
        /// ya incluye los parámetros de paginación cuando aplica) y deserializa la respuesta.
        /// </summary>
        private async Task<DynamicDataSet> FetchInternalAsync(String apiName, IDictionary<String, String> filters, CancellationToken cancellationToken)
        {
            var apiConfig = _settings.Apis[apiName];

            var query = String.Join("&", filters.Select(f => $"{Uri.EscapeDataString(f.Key)}={Uri.EscapeDataString(f.Value)}"));
            var url = String.IsNullOrEmpty(query)
                ? apiConfig.DataEndpoint
                : $"{apiConfig.DataEndpoint}?{query}";

            _logger.LogInformation("Consultando API externo: {Url}", url);

            // Obtiene el HttpClient previamente registrado
            HttpClient httpClient = _httpClientFactory.CreateClient(apiName);

            using var response = await httpClient.GetAsync(url, cancellationToken);

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
                return DynamicDataSet.Empty;

            DynamicDataSet domainDataSet = dto.ToDomainDataSet();
            return domainDataSet;
        }
    }
}
