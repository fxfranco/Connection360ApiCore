using Connection360.Observability.Application.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace Connection360.Observability.Application.DependencyInjection
{
    /// <summary>
    /// Para procesos de corta vida (como el ETL), que construyen el Host pero nunca lo "arrancan":
    /// <c>await using var observability = await host.Services.StartObservabilityAsync();</c> inicia
    /// los recolectores y escritores, y al terminar el bloque (aunque haya excepción) los detiene
    /// vaciando las colas, de modo que no se pierda la telemetría del final de la ejecución.
    /// En una API web NO hace falta: el Host ya gestiona el ciclo de vida.
    /// </summary>
    public sealed class ObservabilityLifetime : IAsyncDisposable
    {
        private readonly IReadOnlyList<IObservabilityComponent> _components;

        internal ObservabilityLifetime(IReadOnlyList<IObservabilityComponent> components) => _components = components;

        public async ValueTask DisposeAsync()
        {
            // Orden inverso al de arranque: primero los recolectores, después los escritores que vacían las colas.
            foreach (IObservabilityComponent component in _components.Reverse())
            {
                try
                {
                    await component.StopAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                    // Apagar la observabilidad nunca debe fallar el proceso.
                }
            }
        }
    }

    public static class ObservabilityLifetimeExtensions
    {
        public static async Task<ObservabilityLifetime> StartObservabilityAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
        {
            List<IObservabilityComponent> components = services.GetServices<IObservabilityComponent>().ToList();
            foreach (IObservabilityComponent component in components)
            {
                try
                {
                    await component.StartAsync(cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // Una falla al arrancar la observabilidad no debe impedir que el proceso haga su trabajo.
                }
            }

            return new ObservabilityLifetime(components);
        }
    }
}
