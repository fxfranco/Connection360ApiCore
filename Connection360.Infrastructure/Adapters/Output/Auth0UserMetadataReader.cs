using Connection360.Domain.Dtos;
using System.Collections;
using System.Globalization;
using System.Text.Json;

namespace Connection360.Infrastructure.Adapters.Output
{
    /// <summary>
    /// Lee de <c>user_metadata</c> de Auth0 los datos de negocio del usuario: <c>company</c>, <c>document</c>,
    /// <c>fullName</c> y <c>phone</c> (de este último, la propiedad <c>number</c>). Es independiente de la red para poder
    /// probarlo sin llamar a Auth0.
    ///
    /// El SDK entrega cada valor del diccionario como <c>object</c>; según cómo se deserialice puede ser un
    /// <see cref="JsonElement"/>, un texto, un número o un diccionario, por eso la lectura admite todas esas formas.
    /// Las claves se comparan sin distinguir mayúsculas de minúsculas.
    /// </summary>
    public static class Auth0UserMetadataReader
    {
        public const String CompanyKey = "company";
        public const String DocumentKey = "document";
        public const String FullNameKey = "fullName";
        public const String PhoneKey = "phone";
        public const String PhoneNumberKey = "number";

        /// <summary>Llena Company, Document, FullName y Phone del usuario a partir de su <c>user_metadata</c>.</summary>
        public static void Apply(Auth0UserDto user, IDictionary<String, Object>? userMetadata)
        {
            user.Company = GetString(userMetadata, CompanyKey);
            user.Document = GetString(userMetadata, DocumentKey);
            user.FullName = GetString(userMetadata, FullNameKey);
            user.Phone = GetPhoneNumber(userMetadata);
        }

        /// <summary>Texto de la clave indicada, o <c>null</c> si no existe, es nula o está vacía.</summary>
        public static String? GetString(IDictionary<String, Object>? userMetadata, String key)
        {
            return ValueToString(FindValue(userMetadata, key));
        }

        /// <summary>
        /// Número de <c>user_metadata.phone</c>. Lo normal es un objeto con la propiedad <c>number</c>; si <c>phone</c> viniera
        /// como un texto o un número simple se devuelve tal cual.
        /// </summary>
        public static String? GetPhoneNumber(IDictionary<String, Object>? userMetadata)
        {
            Object? phone = FindValue(userMetadata, PhoneKey);

            return phone switch
            {
                null => null,
                JsonElement { ValueKind: JsonValueKind.Object } element => ValueToString(FindInElement(element, PhoneNumberKey)),
                IDictionary<String, Object> dictionary => ValueToString(FindValue(dictionary, PhoneNumberKey)),
                IDictionary legacy => ValueToString(FindInLegacyDictionary(legacy, PhoneNumberKey)),
                _ => ValueToString(phone)
            };
        }

        private static Object? FindValue(IDictionary<String, Object>? dictionary, String key)
        {
            if (dictionary == null)
            {
                return null;
            }

            return dictionary
                .Where(entry => String.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase))
                .Select(entry => entry.Value)
                .FirstOrDefault();
        }

        private static Object? FindInLegacyDictionary(IDictionary dictionary, String key)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key is String text && String.Equals(text, key, StringComparison.OrdinalIgnoreCase))
                {
                    return entry.Value;
                }
            }

            return null;
        }

        private static JsonElement? FindInElement(JsonElement element, String key)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (String.Equals(property.Name, key, StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value;
                }
            }

            return null;
        }

        private static String? ValueToString(Object? value)
        {
            String? text = value switch
            {
                null => null,
                String s => s,
                JsonElement element => ElementToString(element),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture)
            };

            return String.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        private static String? ElementToString(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.GetRawText(),
                JsonValueKind.True => Boolean.TrueString,
                JsonValueKind.False => Boolean.FalseString,
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                _ => element.GetRawText()
            };
        }
    }
}
