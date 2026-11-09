using Connection360.Etl.Application.UseCases;
using FluentAssertions;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Connection360.Etl.Application.Tests.UseCases
{
    public class EtlErrorSanitizerTests
    {
        [Fact]
        public void Sanitize_ErrorNulo_DevuelveMotivoDesconocido()
        {
            EtlErrorSanitizer.Sanitize(null).Should().Be(EtlErrorSanitizer.UnknownReason);
        }

        [Fact]
        public void Sanitize_RespuestaConErrorHttpDelGateway_ConservaElStatus()
        {
            var error = new HttpRequestException("El API externo respondió con status 503");

            EtlErrorSanitizer.Sanitize(error).Should().Be("El API externo respondió con status 503");
        }

        [Fact]
        public void Sanitize_HttpRequestExceptionConStatusCode_UsaElStatusSinElMensajeOriginal()
        {
            var error = new HttpRequestException("Response status code does not indicate success: 401 (Unauthorized) https://api.interna.com/v1?key=abc", null, HttpStatusCode.Unauthorized);

            String result = EtlErrorSanitizer.Sanitize(error);

            result.Should().Be("El API externo respondió con status 401");
        }

        [Fact]
        public void Sanitize_HttpRequestExceptionSinStatus_DescribeFalloDeConexionSinExponerElServidor()
        {
            var error = new HttpRequestException("No se puede establecer una conexión (api.proveedor.com:443)", new SocketException(10061));

            String result = EtlErrorSanitizer.Sanitize(error);

            result.Should().Be(EtlErrorSanitizer.ConnectionFailedReason);
            result.Should().NotContain("proveedor");
        }

        [Fact]
        public void Sanitize_SocketException_DescribeFalloDeConexion()
        {
            EtlErrorSanitizer.Sanitize(new SocketException(10060)).Should().Be(EtlErrorSanitizer.ConnectionFailedReason);
        }

        [Theory]
        [InlineData(typeof(TimeoutException))]
        [InlineData(typeof(TaskCanceledException))]
        [InlineData(typeof(OperationCanceledException))]
        public void Sanitize_TiempoDeEspera_DescribeElTiempoAgotado(Type tipo)
        {
            var error = (Exception)Activator.CreateInstance(tipo, "The request was canceled due to the configured HttpClient.Timeout of 100 seconds elapsing.")!;

            EtlErrorSanitizer.Sanitize(error).Should().Be(EtlErrorSanitizer.TimeoutReason);
        }

        [Fact]
        public void Sanitize_ErrorExternoReconocido_TienePrioridadSobreElInterno()
        {
            var error = new HttpRequestException("fallo de red", new TimeoutException("agotado"));

            // Se recorre la cadena del error más externo al más interno: el primero que se reconoce define el motivo.
            EtlErrorSanitizer.Sanitize(error).Should().Be(EtlErrorSanitizer.ConnectionFailedReason);
        }

        [Fact]
        public void Sanitize_RespuestaConFormatoInvalido_DescribeElFormato()
        {
            var error = new JsonException("The JSON value could not be converted to System.Int32. Path: $.data[0].id | LineNumber: 0 | BytePositionInLine: 12.");

            EtlErrorSanitizer.Sanitize(error).Should().Be(EtlErrorSanitizer.InvalidResponseReason);
        }

        [Fact]
        public void Sanitize_ErrorEnvueltoEnOtraExcepcion_ReconoceElInterno()
        {
            var error = new InvalidOperationException("Falló la consulta", new JsonException("json roto"));

            EtlErrorSanitizer.Sanitize(error).Should().Be(EtlErrorSanitizer.InvalidResponseReason);
        }

        [Fact]
        public void Sanitize_ErrorDeSeguridad_DescribeLaConexionSegura()
        {
            var error = new AuthenticationException("The remote certificate is invalid for host api.proveedor.com");

            EtlErrorSanitizer.Sanitize(error).Should().Be(EtlErrorSanitizer.SecureConnectionReason);
        }

        [Fact]
        public void Sanitize_ErrorSimple_ConservaElMotivo()
        {
            EtlErrorSanitizer.Sanitize(new InvalidOperationException("Respuesta vacía del API externo.")).Should().Be("Respuesta vacía del API externo.");
        }

        [Fact]
        public void Sanitize_ConfiguracionFaltante_ConservaElNombreDeLaApiYCambiaComillasSimples()
        {
            var error = new ArgumentException("La API 'BPMS' no existe en el appsettings.");

            EtlErrorSanitizer.Sanitize(error).Should().Be("La API \"BPMS\" no existe en el appsettings.");
        }

        [Fact]
        public void Sanitize_ErrorSinMensajeUtil_IndicaElTipo()
        {
            var error = new CustomEmptyMessageException();

            EtlErrorSanitizer.Sanitize(error).Should().Be($"{EtlErrorSanitizer.UnknownReason} ({nameof(CustomEmptyMessageException)})");
        }

        [Theory]
        [InlineData("Fallo al llamar https://api.interna.com/v1/data?token=abc123 por favor", "Fallo al llamar [url] por favor")]
        [InlineData("No responde http://10.20.30.40:8080/health", "No responde [url]")]
        [InlineData("Servidor 10.20.30.40:5432 rechazó la conexión", "Servidor [ip] rechazó la conexión")]
        [InlineData("Notificar a soporte@empresa.com del fallo", "Notificar a [correo] del fallo")]
        [InlineData("No se pudo resolver api.proveedor.com:443", "No se pudo resolver [servidor]")]
        [InlineData(@"No se encontró C:\Config\secretos.json en el equipo", "No se encontró [ruta] en el equipo")]
        [InlineData("No se encontró /var/app/config/secretos.json en el equipo", "No se encontró [ruta] en el equipo")]
        public void SanitizeText_EnmascaraDatosSensibles(String texto, String esperado)
        {
            EtlErrorSanitizer.SanitizeText(texto).Should().Be(esperado);
        }

        [Theory]
        [InlineData("password=Sup3rS3cret!", "password=[oculto]")]
        [InlineData("Server=db.interna.com;Database=conexion;User Id=sa;Password=Clave123", "Server=[oculto];Database=[oculto];User Id=[oculto];Password=[oculto]")]
        [InlineData("apikey: abcdef y token = zzz", "apikey=[oculto] y token=[oculto]")]
        [InlineData("Authorization: Bearer abc.def-ghi_123", "Authorization=[oculto]")]
        public void SanitizeText_EnmascaraCredencialesYCadenasDeConexion(String texto, String esperado)
        {
            EtlErrorSanitizer.SanitizeText(texto).Should().Be(esperado);
        }

        [Fact]
        public void SanitizeText_EnmascaraTokensJwtYCadenasOpacasLargas()
        {
            // El JWT se arma en tiempo de ejecución (encabezado.carga.firma en Base64Url) para que no exista como literal en el código
            // y los escáneres de secretos (GitLeaks) no lo confundan con un token real.
            String jwt = String.Join('.', Base64Url("{\"alg\":\"HS256\"}"), Base64Url("{\"sub\":\"123456\"}"), Base64Url("signature"));
            String opaque = new('a', 40);

            EtlErrorSanitizer.SanitizeText($"token inválido {jwt} y llave {opaque}").Should().Be("token inválido [oculto] y llave [oculto]");
        }

        private static String Base64Url(String text) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(text)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        [Fact]
        public void SanitizeText_NoTocaNombresDeTipoNiTextoNormal()
        {
            EtlErrorSanitizer.SanitizeText("System.Net.Http.HttpRequestException: la API respondió mal").Should().Be("System.Net.Http.HttpRequestException: la API respondió mal");
        }

        [Fact]
        public void SanitizeText_QuitaSaltosDeLineaYEspaciosRepetidos()
        {
            EtlErrorSanitizer.SanitizeText("linea uno\r\n   linea\tdos\n").Should().Be("linea uno linea dos");
        }

        [Fact]
        public void SanitizeText_CambiaLasComillasSimplesPorDobles()
        {
            EtlErrorSanitizer.SanitizeText("campo 'estado' inválido").Should().Be("campo \"estado\" inválido");
        }

        [Fact]
        public void SanitizeText_TextoLargo_SeRecortaAlMaximo()
        {
            String result = EtlErrorSanitizer.SanitizeText(String.Join(" ", Enumerable.Repeat("palabra", 100)));

            result.Length.Should().BeLessThanOrEqualTo(EtlErrorSanitizer.MaxLength);
            result.Should().EndWith("...");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void SanitizeText_SinTexto_DevuelveVacio(String? texto)
        {
            EtlErrorSanitizer.SanitizeText(texto).Should().BeEmpty();
        }

        [Fact]
        public void Sanitize_NuncaIncluyeElStackTrace()
        {
            Exception error;
            try { throw new InvalidOperationException("falla controlada"); }
            catch (Exception ex) { error = ex; }

            EtlErrorSanitizer.Sanitize(error).Should().Be("falla controlada").And.NotContain("   at ");
        }

        private sealed class CustomEmptyMessageException : Exception
        {
            public override String Message => String.Empty;
        }
    }
}
