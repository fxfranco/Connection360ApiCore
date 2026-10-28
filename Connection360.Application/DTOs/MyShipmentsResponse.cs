using Connection360.Domain.Entities;
using Connection360.Domain.Services;

namespace Connection360.Application.DTOs
{
    /// <summary>
    /// Resultado de las consultas de "mis envíos" (listado completo, filtrado, historial completo e
    /// historial filtrado) expuestas por <c>MyShipmentsController</c>.
    /// </summary>
    public class MyShipmentsResponse
    {
        /// <summary>Datos resumen/listado de envíos del cliente para la consulta realizada.</summary>
        public ClientSummaryResponse ClientSummaryResponseData { get; set; } = new();
    }
}
