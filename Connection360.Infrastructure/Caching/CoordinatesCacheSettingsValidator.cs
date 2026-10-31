using Microsoft.Extensions.Options;

namespace Connection360.Infrastructure.Caching
{
    /// <summary>
    /// Valida la sección "CoordinatesCache" para fallar al arrancar (y no en la primera petición)
    /// si alguien configura un valor que dejaría la caché sin límite o inutilizable.
    /// </summary>
    public sealed class CoordinatesCacheSettingsValidator : IValidateOptions<CoordinatesCacheSettings>
    {
        public ValidateOptionsResult Validate(String? name, CoordinatesCacheSettings options)
        {
            List<String> errors = new();

            if (options.MaxSizeBytes <= 0)
                errors.Add($"{CoordinatesCacheSettings.SectionName}:MaxSizeBytes debe ser mayor que 0 (para desactivar la caché use Enabled=false).");

            if (options.EvictionPercentage is <= 0 or > 1)
                errors.Add($"{CoordinatesCacheSettings.SectionName}:EvictionPercentage debe estar en el rango (0, 1].");

            if (options.AbsoluteExpiration <= TimeSpan.Zero)
                errors.Add($"{CoordinatesCacheSettings.SectionName}:AbsoluteExpiration debe ser mayor que cero.");

            if (options.SlidingExpiration is { } sliding && sliding <= TimeSpan.Zero)
                errors.Add($"{CoordinatesCacheSettings.SectionName}:SlidingExpiration debe ser mayor que cero o no estar definido.");

            if (options.MaxKeyLength <= 0)
                errors.Add($"{CoordinatesCacheSettings.SectionName}:MaxKeyLength debe ser mayor que 0.");

            return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
        }
    }
}
