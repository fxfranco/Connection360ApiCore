using Microsoft.Extensions.Hosting;

namespace Connection360.Observability.Application.Pipeline
{
    /// <summary>
    /// Marca los servicios en segundo plano de la observabilidad (escritores, recolectores,
    /// inicializador de almacenamientos). Permite arrancarlos y detenerlos por separado en procesos
    /// de corta vida (como el ETL) que no ejecutan el ciclo de vida completo del Host.
    /// </summary>
    public interface IObservabilityComponent : IHostedService { }
}
