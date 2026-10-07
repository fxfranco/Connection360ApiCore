using System.Globalization;

namespace Connection360.Observability.Application
{
    /// <summary>
    /// Normaliza valores de atributos/etiquetas a tipos simples que cualquier almacenamiento sabe
    /// guardar (texto, números, booleanos, fechas). Cualquier otro objeto se convierte a texto en el
    /// momento de capturarlo, así no se retienen objetos mutables ni se serializan grafos enormes.
    /// </summary>
    public static class TelemetryValue
    {
        public static Object? Normalize(Object? value, Int32 maxLength)
        {
            switch (value)
            {
                case null:
                    return null;
                case String text:
                    return Truncate(text, maxLength);
                case Boolean or Byte or SByte or Int16 or UInt16 or Int32 or Int64 or Double or Single:
                    return value;
                case UInt32 u32:
                    return (Int64)u32;
                case UInt64 u64:
                    return u64 <= Int64.MaxValue ? (Int64)u64 : u64.ToString(CultureInfo.InvariantCulture);
                case Decimal dec:
                    return (Double)dec;
                case DateTime dt:
                    return dt.Kind == DateTimeKind.Local ? dt.ToUniversalTime() : dt;
                case DateTimeOffset dto:
                    return dto.UtcDateTime;
                case Guid guid:
                    return guid.ToString();
                case TimeSpan span:
                    return span.TotalMilliseconds;
                case Enum e:
                    return e.ToString();
                case Array array when array.Length <= 32:
                    var items = new List<Object?>(array.Length);
                    foreach (Object? item in array)
                    {
                        items.Add(Normalize(item, maxLength));
                    }

                    return items;
                default:
                    return Truncate(Convert.ToString(value, CultureInfo.InvariantCulture) ?? value.GetType().Name, maxLength);
            }
        }

        public static String Truncate(String text, Int32 maxLength)
            => maxLength > 0 && text.Length > maxLength ? text[..maxLength] : text;
    }
}
