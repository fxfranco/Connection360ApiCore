namespace Connection360Notification.Infrastructure.Sms
{
    /// <summary>
    /// Normaliza el celular guardado en basicUserData (<c>user_metadata.phone.number</c>) al formato que pide Brevo:
    /// indicativo de país + número, solo dígitos y sin "+" (p. ej. 573113232323).
    /// </summary>
    public static class SmsPhoneNumber
    {
        private const Int32 MinDigits = 8;
        private const Int32 MaxDigits = 15;
        private const Int32 MaxNationalDigits = 10;

        /// <summary>
        /// Reglas: se ignoran espacios, guiones, paréntesis y puntos. Con "+" o "00" al inicio el número ya trae indicativo y se
        /// respeta. Sin ellos, si tiene hasta 10 dígitos (número nacional) se antepone <paramref name="defaultCountryCode"/>;
        /// si tiene más se asume que ya incluye el indicativo. Devuelve <c>null</c> si queda vacío, con caracteres inválidos
        /// o con una longitud fuera de 8 a 15 dígitos (norma E.164).
        /// </summary>
        public static String? Normalize(String? phone, String? defaultCountryCode)
        {
            if (String.IsNullOrWhiteSpace(phone))
            {
                return null;
            }

            String text = phone.Trim();
            Boolean international = text.StartsWith('+');
            if (international)
            {
                text = text.Substring(1);
            }

            var digits = new System.Text.StringBuilder(text.Length);
            foreach (Char c in text)
            {
                if (Char.IsAsciiDigit(c))
                {
                    digits.Append(c);
                }
                else if (c is not (' ' or '-' or '(' or ')' or '.'))
                {
                    return null;
                }
            }

            String number = digits.ToString();
            if (!international && number.StartsWith("00", StringComparison.Ordinal))
            {
                number = number.Substring(2);
                international = true;
            }

            if (!international && number.Length <= MaxNationalDigits)
            {
                String country = new String((defaultCountryCode ?? String.Empty).Where(Char.IsAsciiDigit).ToArray());
                number = country + number;
            }

            return number.Length is >= MinDigits and <= MaxDigits ? number : null;
        }
    }
}
