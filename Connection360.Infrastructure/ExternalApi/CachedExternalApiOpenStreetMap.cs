using Connection360.Application.DTOs;
using Connection360.Application.Ports;
using Connection360.Infrastructure.Caching;

namespace Connection360.Infrastructure.ExternalApi
{
    /// <summary>
    /// Decorador de <see cref="IExternalApiOpenStreetMap"/> que consulta primero la caché de coordenadas y
    /// solo llama al adaptador real (<see cref="ExternalApiOpenStreetMap"/>) cuando el lugar no está guardado.
    /// El caso de uso sigue dependiendo únicamente del puerto: no sabe que existe una caché (Open/Closed y
    /// Dependency Inversion), y la caché se puede quitar desde configuración sin tocar la capa de aplicación.
    /// </summary>
    public sealed class CachedExternalApiOpenStreetMap : IExternalApiOpenStreetMap
    {
        private readonly IExternalApiOpenStreetMap _inner;
        private readonly ICoordinatesCache _cache;

        public CachedExternalApiOpenStreetMap(IExternalApiOpenStreetMap inner, ICoordinatesCache cache)
        {
            _inner = inner;
            _cache = cache;
        }

        public Task<OpenStreetMapDto> GetCoordinates(String apiName, String PlaceName, CancellationToken cancellationToken)
        {
            // Sin lugar no hay clave: se delega tal cual para conservar el comportamiento original del adaptador.
            if (String.IsNullOrWhiteSpace(PlaceName))
            {
                return _inner.GetCoordinates(apiName, PlaceName, cancellationToken);
            }

            // La clave incluye el nombre de la API configurada: dos proveedores distintos no comparten resultados.
            // El lugar se recorta aquí (y no solo en la caché) porque los espacios quedarían en medio de la clave
            // compuesta "api|lugar" y "Bogota" y " Bogota " serían entradas distintas.
            String key = $"{apiName}|{PlaceName.Trim()}";
            return _cache.GetOrCreateAsync(key, token => _inner.GetCoordinates(apiName, PlaceName, token), cancellationToken);
        }
    }
}
