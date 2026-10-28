using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Connection360.Etl.Infrastructure.ExternalApi
{
    /// <summary>
    /// Validación de certificados TLS que se usa SOLO cuando se activa <c>ExternalApi:AllowInvalidServerCertificate</c>
    /// (entornos de desarrollo). En lugar de aceptar cualquier certificado, es una validación PARCIAL: tolera únicamente
    /// lo que es normal en desarrollo y rechaza lo que indica un certificado realmente dañino.
    ///
    /// Se tolera: nombre que no coincide con el host (p. ej. certificado emitido para "localhost" al consumir por IP) y
    /// cadena no confiable (autofirmado o CA desconocida, <see cref="X509ChainStatusFlags.UntrustedRoot"/> /
    /// <see cref="X509ChainStatusFlags.PartialChain"/>), así como no poder consultar la revocación (sin acceso a internet).
    ///
    /// Se rechaza: que el servidor no presente certificado, certificados vencidos o aún no vigentes, revocados, con firma
    /// inválida, con restricciones o usos incorrectos, etc. Con el valor por defecto (<c>false</c>) este validador no
    /// interviene y se aplica la validación estándar de .NET.
    /// </summary>
    public static class DevelopmentServerCertificateValidator
    {
        /// <summary>Estados de la cadena que se aceptan en desarrollo (cualquier otro estado se rechaza).</summary>
        public const X509ChainStatusFlags ToleratedChainStatus =
            X509ChainStatusFlags.NoError
            | X509ChainStatusFlags.UntrustedRoot
            | X509ChainStatusFlags.PartialChain
            | X509ChainStatusFlags.RevocationStatusUnknown
            | X509ChainStatusFlags.OfflineRevocation;

        /// <summary>
        /// Firma compatible con <see cref="HttpClientHandler.ServerCertificateCustomValidationCallback"/>.
        /// </summary>
        public static Boolean Validate(HttpRequestMessage request, X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            IEnumerable<X509ChainStatusFlags>? chainStatuses = chain?.ChainStatus.Select(s => s.Status);
            return Evaluate(certificate is not null, sslPolicyErrors, chainStatuses);
        }

        /// <summary>Regla de decisión, sin dependencias de .NET TLS para poder probarla directamente.</summary>
        /// <param name="hasCertificate">Si el servidor presentó un certificado.</param>
        /// <param name="sslPolicyErrors">Errores que reportó la plataforma.</param>
        /// <param name="chainStatuses">Estado de cada elemento de la cadena (<c>null</c> si no hay cadena).</param>
        public static Boolean Evaluate(Boolean hasCertificate, SslPolicyErrors sslPolicyErrors, IEnumerable<X509ChainStatusFlags>? chainStatuses)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            if (!hasCertificate || sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
            {
                return false;
            }

            if (!sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
            {
                // Solo el nombre no coincide.
                return true;
            }

            return chainStatuses is not null && chainStatuses.All(status => (status & ~ToleratedChainStatus) == 0);
        }
    }
}
