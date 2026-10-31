using Connection360.Application.DTOs;

namespace Connection360.Infrastructure.Caching
{
    /// <summary>
    /// Almacén de coordenadas por lugar. Separa la política de caché (límite de memoria, expiración,
    /// evicción) del adaptador que consulta el API externo, de modo que el decorador
    /// <see cref="ExternalApi.CachedExternalApiOpenStreetMap"/> dependa de una abstracción y no de IMemoryCache.
    /// </summary>
    public interface ICoordinatesCache
    {
        /// <summary>
        /// Devuelve las coordenadas cacheadas del lugar o, si no existen, ejecuta <paramref name="factory"/>
        /// (consulta al API externo), guarda el resultado y lo devuelve. Si <paramref name="factory"/> lanza una
        /// excepción, no se guarda nada y la excepción se propaga.
        /// </summary>
        Task<OpenStreetMapDto> GetOrCreateAsync(String key, Func<CancellationToken, Task<OpenStreetMapDto>> factory, CancellationToken cancellationToken);
    }
}
