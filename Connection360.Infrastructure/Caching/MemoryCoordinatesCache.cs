using Connection360.Application.DTOs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Connection360.Infrastructure.Caching
{
    /// <summary>
    /// <see cref="ICoordinatesCache"/> sobre una instancia DEDICADA de <see cref="MemoryCache"/> (no la
    /// compartida de la aplicación), registrada como singleton:
    ///   - Límite de memoria: cada entrada declara su tamaño estimado en bytes y el cache tiene SizeLimit.
    ///     Al usar una instancia propia, el límite no obliga a otros consumidores de IMemoryCache a declarar Size.
    ///   - Evicción: antes de insertar una entrada que no cabe se libera un porcentaje de la caché; MemoryCache
    ///     elimina primero por prioridad y luego por último acceso (las más viejas), y la entrada nueva siempre
    ///     queda guardada (sin la compactación previa, MemoryCache rechazaría la entrada nueva al estar llena).
    ///   - Expiración absoluta y deslizante para que no se acumulen lugares que ya no se consultan.
    ///   - Una misma clave no se consulta dos veces en paralelo (locks por franjas), lo que además respeta
    ///     el límite de peticiones de Nominatim cuando varios usuarios abren el mismo envío a la vez.
    ///   - Se guardan y devuelven copias del DTO: el DTO es mutable y un consumidor no debe poder alterar la caché.
    /// </summary>
    public sealed class MemoryCoordinatesCache : ICoordinatesCache, IDisposable
    {
        /// <summary>Sobrecosto aproximado (bytes) del objeto, la entrada de MemoryCache y sus referencias.</summary>
        private const Int64 EntryOverheadBytes = 256;

        /// <summary>Cantidad de semáforos usados para serializar consultas de la misma clave (memoria constante).</summary>
        private const Int32 LockStripes = 32;

        private readonly MemoryCache _cache;
        private readonly CoordinatesCacheSettings _settings;
        private readonly ILogger<MemoryCoordinatesCache> _logger;
        private readonly SemaphoreSlim[] _locks;

        public MemoryCoordinatesCache(IOptions<CoordinatesCacheSettings> settings, ILogger<MemoryCoordinatesCache> logger)
        {
            _settings = settings.Value;
            _logger = logger;

            _cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = _settings.MaxSizeBytes,
                CompactionPercentage = _settings.EvictionPercentage,
                TrackStatistics = true
            });

            _locks = new SemaphoreSlim[LockStripes];
            for (Int32 i = 0; i < _locks.Length; i++)
            {
                _locks[i] = new SemaphoreSlim(1, 1);
            }
        }

        /// <summary>Cantidad de lugares guardados actualmente.</summary>
        public Int32 Count => _cache.Count;

        /// <summary>Memoria estimada (bytes) usada actualmente por la caché.</summary>
        public Int64 CurrentSizeBytes => _cache.GetCurrentStatistics()?.CurrentEstimatedSize ?? 0;

        /// <summary>
        /// Tamaño estimado (bytes) de una entrada: texto en UTF-16 (2 bytes por carácter) de la clave y de los
        /// tres campos del DTO, más un sobrecosto fijo. Es una estimación conservadora, no una medición exacta.
        /// </summary>
        public static Int64 EstimateEntrySize(String key, OpenStreetMapDto value)
        {
            Int64 characters = (Int64)key.Length + value.PlaceName.Length + value.Latitud.Length + value.Longitud.Length;
            return EntryOverheadBytes + (characters * sizeof(Char));
        }

        public async Task<OpenStreetMapDto> GetOrCreateAsync(String key, Func<CancellationToken, Task<OpenStreetMapDto>> factory, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(factory);

            String cacheKey = NormalizeKey(key);

            // Claves anómalas (demasiado largas) no se cachean: se consulta siempre al API externo.
            if (cacheKey.Length > _settings.MaxKeyLength)
            {
                _logger.LogWarning("Clave de coordenadas de {Length} caracteres supera el máximo de {Max}; no se cachea", cacheKey.Length, _settings.MaxKeyLength);
                return await factory(cancellationToken);
            }

            OpenStreetMapDto? cached;
            if (TryGet(cacheKey, out cached))
            {
                _logger.LogDebug("Coordenadas obtenidas de la caché para '{Key}'", cacheKey);
                return cached!;
            }

            SemaphoreSlim keyLock = _locks[(UInt32)StringComparer.Ordinal.GetHashCode(cacheKey) % LockStripes];
            await keyLock.WaitAsync(cancellationToken);
            try
            {
                // Otra petición pudo haber llenado la caché mientras esperábamos el lock.
                if (TryGet(cacheKey, out cached))
                {
                    return cached!;
                }

                _logger.LogDebug("Coordenadas no encontradas en la caché para '{Key}'; consultando el API externo", cacheKey);
                OpenStreetMapDto result = await factory(cancellationToken);

                Store(cacheKey, result);
                return Copy(result);
            }
            finally
            {
                keyLock.Release();
            }
        }

        public void Dispose()
        {
            _cache.Dispose();
            foreach (SemaphoreSlim semaphore in _locks)
            {
                semaphore.Dispose();
            }
        }

        /// <summary>El lugar se compara sin distinguir mayúsculas/minúsculas ni espacios en los extremos.</summary>
        private static String NormalizeKey(String key) => key.Trim().ToUpperInvariant();

        private static OpenStreetMapDto Copy(OpenStreetMapDto source) => new()
        {
            PlaceName = source.PlaceName,
            Latitud = source.Latitud,
            Longitud = source.Longitud
        };

        private Boolean TryGet(String cacheKey, out OpenStreetMapDto? copy)
        {
            if (_cache.TryGetValue(cacheKey, out OpenStreetMapDto? stored) && stored is not null)
            {
                copy = Copy(stored);
                return true;
            }

            copy = null;
            return false;
        }

        private void Store(String cacheKey, OpenStreetMapDto value)
        {
            Int64 size = EstimateEntrySize(cacheKey, value);

            // Una sola entrada mayor que todo el límite nunca cabría: se responde sin cachear.
            if (size > _settings.MaxSizeBytes)
            {
                _logger.LogWarning("La entrada de coordenadas para '{Key}' ({Size} bytes) supera el límite de la caché; no se cachea", cacheKey, size);
                return;
            }

            // Si no cabe, liberar primero las entradas más viejas (menos usadas recientemente).
            MakeRoom(size);

            using ICacheEntry entry = _cache.CreateEntry(cacheKey);
            entry.Value = Copy(value);
            entry.Size = size;
            entry.Priority = CacheItemPriority.Normal;
            entry.AbsoluteExpirationRelativeToNow = _settings.AbsoluteExpiration;
            if (_settings.SlidingExpiration is { } sliding)
            {
                entry.SlidingExpiration = sliding;
            }
        }

        /// <summary>
        /// Libera memoria antes de insertar. MemoryCache.Compact(porcentaje) trabaja sobre la CANTIDAD de entradas
        /// (elimina primero por prioridad y luego las de último acceso más antiguo) y con pocas entradas el
        /// porcentaje puede redondear a 0, por eso se exige liberar al menos una entrada y se repite si hace falta
        /// (los tamaños de las entradas no son idénticos y otras peticiones pueden estar insertando).
        /// </summary>
        private void MakeRoom(Int64 requiredBytes)
        {
            for (Int32 attempt = 0; attempt < 3 && CurrentSizeBytes + requiredBytes > _settings.MaxSizeBytes && _cache.Count > 0; attempt++)
            {
                _logger.LogInformation("Caché de coordenadas al límite ({Current}/{Max} bytes); liberando las entradas más viejas",
                    CurrentSizeBytes, _settings.MaxSizeBytes);

                Double atLeastOneEntry = (1.0 / _cache.Count) + 0.001;
                _cache.Compact(Math.Min(1.0, Math.Max(_settings.EvictionPercentage, atLeastOneEntry)));
            }
        }
    }
}
