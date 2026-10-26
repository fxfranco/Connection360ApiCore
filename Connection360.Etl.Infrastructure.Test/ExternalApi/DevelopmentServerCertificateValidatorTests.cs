using Connection360.Etl.Infrastructure.ExternalApi;
using FluentAssertions;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Connection360.Etl.Infrastructure.Tests.ExternalApi
{
    public class DevelopmentServerCertificateValidatorTests
    {
        private static readonly X509ChainStatusFlags[] Ninguno = Array.Empty<X509ChainStatusFlags>();

        [Fact]
        public void Evaluate_SinErrores_Acepta()
        {
            DevelopmentServerCertificateValidator.Evaluate(true, SslPolicyErrors.None, Ninguno).Should().BeTrue();
        }

        [Fact]
        public void Evaluate_SoloNombreNoCoincide_Acepta()
        {
            DevelopmentServerCertificateValidator.Evaluate(true, SslPolicyErrors.RemoteCertificateNameMismatch, null).Should().BeTrue();
        }

        [Theory]
        [InlineData(X509ChainStatusFlags.UntrustedRoot)]
        [InlineData(X509ChainStatusFlags.PartialChain)]
        [InlineData(X509ChainStatusFlags.RevocationStatusUnknown)]
        [InlineData(X509ChainStatusFlags.OfflineRevocation)]
        [InlineData(X509ChainStatusFlags.UntrustedRoot | X509ChainStatusFlags.RevocationStatusUnknown)]
        public void Evaluate_ErroresDeCadenaTipicosDeDesarrollo_Acepta(X509ChainStatusFlags estado)
        {
            DevelopmentServerCertificateValidator
                .Evaluate(true, SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch, new[] { estado })
                .Should().BeTrue();
        }

        [Theory]
        [InlineData(X509ChainStatusFlags.NotTimeValid)]
        [InlineData(X509ChainStatusFlags.Revoked)]
        [InlineData(X509ChainStatusFlags.NotSignatureValid)]
        [InlineData(X509ChainStatusFlags.InvalidBasicConstraints)]
        [InlineData(X509ChainStatusFlags.NotValidForUsage)]
        [InlineData(X509ChainStatusFlags.UntrustedRoot | X509ChainStatusFlags.NotTimeValid)]
        public void Evaluate_CertificadoVencidoRevocadoOInvalido_Rechaza(X509ChainStatusFlags estado)
        {
            DevelopmentServerCertificateValidator
                .Evaluate(true, SslPolicyErrors.RemoteCertificateChainErrors, new[] { estado })
                .Should().BeFalse();
        }

        [Fact]
        public void Evaluate_UnElementoMaloEntreVarios_Rechaza()
        {
            DevelopmentServerCertificateValidator
                .Evaluate(true, SslPolicyErrors.RemoteCertificateChainErrors,
                    new[] { X509ChainStatusFlags.UntrustedRoot, X509ChainStatusFlags.Revoked })
                .Should().BeFalse();
        }

        [Fact]
        public void Evaluate_ErrorDeCadenaSinDetalleDeCadena_Rechaza()
        {
            DevelopmentServerCertificateValidator.Evaluate(true, SslPolicyErrors.RemoteCertificateChainErrors, null).Should().BeFalse();
        }

        [Theory]
        [InlineData(SslPolicyErrors.RemoteCertificateNotAvailable)]
        [InlineData(SslPolicyErrors.RemoteCertificateNotAvailable | SslPolicyErrors.RemoteCertificateNameMismatch)]
        public void Evaluate_ServidorSinCertificado_Rechaza(SslPolicyErrors errores)
        {
            DevelopmentServerCertificateValidator.Evaluate(true, errores, Ninguno).Should().BeFalse();
        }

        [Fact]
        public void Evaluate_SinObjetoCertificado_Rechaza()
        {
            DevelopmentServerCertificateValidator.Evaluate(false, SslPolicyErrors.RemoteCertificateNameMismatch, Ninguno).Should().BeFalse();
        }

        private static X509Certificate2 CrearAutofirmado(DateTimeOffset desde, DateTimeOffset hasta)
        {
            using RSA rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return request.CreateSelfSigned(desde, hasta);
        }

        private static X509Chain CrearCadena(X509Certificate2 certificado)
        {
            var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.Build(certificado);
            return chain;
        }

        [Fact]
        public void Validate_CertificadoAutofirmadoVigente_ConErroresDeCadenaYNombre_Acepta()
        {
            using X509Certificate2 certificado = CrearAutofirmado(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
            using X509Chain chain = CrearCadena(certificado);

            DevelopmentServerCertificateValidator
                .Validate(new HttpRequestMessage(), certificado, chain,
                    SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch)
                .Should().BeTrue();
        }

        [Fact]
        public void Validate_CertificadoAutofirmadoVencido_Rechaza()
        {
            using X509Certificate2 certificado = CrearAutofirmado(DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow.AddDays(-1));
            using X509Chain chain = CrearCadena(certificado);

            DevelopmentServerCertificateValidator
                .Validate(new HttpRequestMessage(), certificado, chain, SslPolicyErrors.RemoteCertificateChainErrors)
                .Should().BeFalse();
        }

        [Fact]
        public void Validate_SinCertificadoNiCadena_Rechaza()
        {
            DevelopmentServerCertificateValidator
                .Validate(new HttpRequestMessage(), null, null, SslPolicyErrors.RemoteCertificateNotAvailable)
                .Should().BeFalse();
        }

        [Fact]
        public void Validate_SinErrores_Acepta()
        {
            DevelopmentServerCertificateValidator
                .Validate(new HttpRequestMessage(), null, null, SslPolicyErrors.None)
                .Should().BeTrue();
        }
    }
}
