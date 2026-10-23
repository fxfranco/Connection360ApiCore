using Connection360.Domain.Dtos;
using System.Collections;
using System.Text.Json;

namespace Connection360.Infrastructure.Adapters.Output
{
    /// <summary>
    /// Arma el cambio de <c>user_metadata</c> (company, document, fullName y phone.number) que hay que enviar a Auth0 al
    /// actualizar un usuario. No depende de la red, por eso se puede probar. Es el complemento de
    /// <see cref="Auth0UserMetadataReader"/>, que lee esos mismos datos.
    ///
    /// Reglas:
    /// <list type="bullet">
    /// <item>Solo se incluyen los valores informados (no nulos ni en blanco) y distintos de los actuales; si no hay nada
    /// que cambiar devuelve <c>null</c> y no se llama a Auth0. Un valor en blanco significa "no cambiar", no "borrar".</item>
    /// <item>Auth0 mezcla <c>user_metadata</c> solo en el primer nivel: lo que se envíe en una propiedad reemplaza TODA
    /// esa propiedad. Por eso, para cambiar <c>phone.number</c> se reenvía el objeto <c>phone</c> completo con las demás
    /// propiedades que ya tenía (p. ej. un código de país), y las claves de metadata que no cambian ni se tocan.</item>
    /// <item>Si la clave ya existe con otra combinación de mayúsculas (p. ej. <c>FullName</c>) se reutiliza, para no
    /// dejar dos claves equivalentes.</item>
    /// </list>
    /// </summary>
    public static class Auth0UserMetadataUpdater
    {
        /// <summary>Devuelve las propiedades de <c>user_metadata</c> a enviar, o <c>null</c> si no hay cambios.</summary>
        public static Dictionary<String, Object>? BuildPatch(Auth0UserDto requested, IDictionary<String, Object>? currentMetadata)
        {
            Dictionary<String, Object> patch = new Dictionary<String, Object>(StringComparer.Ordinal);

            AddIfChanged(patch, currentMetadata, Auth0UserMetadataReader.CompanyKey, requested.Company);
            AddIfChanged(patch, currentMetadata, Auth0UserMetadataReader.DocumentKey, requested.Document);
            AddIfChanged(patch, currentMetadata, Auth0UserMetadataReader.FullNameKey, requested.FullName);
            AddPhoneIfChanged(patch, currentMetadata, requested.Phone);

            return patch.Count == 0 ? null : patch;
        }

        private static void AddIfChanged(Dictionary<String, Object> patch, IDictionary<String, Object>? current, String key, String? requested)
        {
            String? currentValue = Auth0UserMetadataReader.GetString(current, key);
            if (Auth0UserUpdatePlanner.HasChanged(requested, currentValue, ignoreCase: false))
            {
                patch[ResolveKey(current, key)] = requested!.Trim();
            }
        }

        private static void AddPhoneIfChanged(Dictionary<String, Object> patch, IDictionary<String, Object>? current, String? requested)
        {
            String? currentNumber = Auth0UserMetadataReader.GetPhoneNumber(current);
            if (!Auth0UserUpdatePlanner.HasChanged(requested, currentNumber, ignoreCase: false))
            {
                return;
            }

            String phoneKey = ResolveKey(current, Auth0UserMetadataReader.PhoneKey);
            Dictionary<String, Object> phone = CopyPhoneObject(current != null && current.TryGetValue(phoneKey, out Object? existing) ? existing : null);

            String numberKey = phone.Keys.FirstOrDefault(k => String.Equals(k, Auth0UserMetadataReader.PhoneNumberKey, StringComparison.OrdinalIgnoreCase))
                ?? Auth0UserMetadataReader.PhoneNumberKey;
            phone[numberKey] = requested!.Trim();

            patch[phoneKey] = phone;
        }

        /// <summary>Copia las propiedades que ya tiene <c>phone</c> (si era un objeto) para no perderlas al reemplazarlo.</summary>
        private static Dictionary<String, Object> CopyPhoneObject(Object? existing)
        {
            Dictionary<String, Object> copy = new Dictionary<String, Object>(StringComparer.Ordinal);

            switch (existing)
            {
                case JsonElement { ValueKind: JsonValueKind.Object } element:
                    foreach (JsonProperty property in element.EnumerateObject())
                    {
                        copy[property.Name] = property.Value;
                    }
                    break;

                case IDictionary<String, Object> dictionary:
                    foreach (KeyValuePair<String, Object> entry in dictionary)
                    {
                        copy[entry.Key] = entry.Value;
                    }
                    break;

                case IDictionary legacy:
                    foreach (DictionaryEntry entry in legacy)
                    {
                        if (entry.Key is String name && entry.Value != null)
                        {
                            copy[name] = entry.Value;
                        }
                    }
                    break;
            }

            return copy;
        }

        /// <summary>Clave tal como ya existe en el metadata (sin distinguir mayúsculas) o la clave canónica si no existe.</summary>
        private static String ResolveKey(IDictionary<String, Object>? current, String canonicalKey)
        {
            if (current != null)
            {
                foreach (String existing in current.Keys)
                {
                    if (String.Equals(existing, canonicalKey, StringComparison.OrdinalIgnoreCase))
                    {
                        return existing;
                    }
                }
            }

            return canonicalKey;
        }
    }
}
