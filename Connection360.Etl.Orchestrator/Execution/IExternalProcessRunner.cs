using Connection360.Etl.Orchestrator.Configuration;

namespace Connection360.Etl.Orchestrator.Execution
{
    /// <summary>
    /// Puerto para lanzar el proceso externo asociado a un trabajo programado. Existe como
    /// interfaz -en vez de usar System.Diagnostics.Process directamente desde
    /// <see cref="CronJobRunnerLoop"/>- para que el bucle de scheduling no dependa de los detalles
    /// de cómo se arranca el proceso ni de cómo se retransmiten sus logs (principio de inversión de
    /// dependencias: el bucle solo conoce "ejecutar este trabajo y decime si salió bien").
    /// </summary>
    public interface IExternalProcessRunner
    {
        /// <summary>
        /// Lanza el proceso configurado en <paramref name="job"/> y espera a que termine,
        /// retransmitiendo en vivo toda su salida estándar/de error hacia el logger del
        /// orquestador. Nunca lanza excepciones por un fallo del proceso lanzado (código de salida
        /// distinto de 0, o cualquier error al iniciarlo) -esos casos se reportan en el
        /// <see cref="ProcessRunResult"/> devuelto-, salvo <see cref="OperationCanceledException"/>
        /// cuando <paramref name="cancellationToken"/> se cancela (apagado del propio orquestador),
        /// que se propaga tal cual.
        /// </summary>
        Task<ProcessRunResult> RunAsync(CronJobSettings job, CancellationToken cancellationToken);
    }
}
