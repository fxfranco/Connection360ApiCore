namespace Connection360.Etl.Orchestrator.Execution
{
    /// <summary>Resultado de una corrida del proceso externo asociado a un <see cref="Configuration.CronJobSettings"/>.</summary>
    /// <param name="Success">true si el proceso terminó con código de salida 0.</param>
    /// <param name="ExitCode">Código de salida del proceso, o null si nunca llegó a arrancar.</param>
    /// <param name="Duration">Duración total de la corrida (desde que se intentó iniciar el proceso hasta que terminó o se mató por timeout/cancelación).</param>
    /// <param name="ErrorMessage">Detalle del error cuando <see cref="Success"/> es false; null si fue exitosa.</param>
    public sealed record ProcessRunResult(Boolean Success, Int32? ExitCode, TimeSpan Duration, String? ErrorMessage);
}
