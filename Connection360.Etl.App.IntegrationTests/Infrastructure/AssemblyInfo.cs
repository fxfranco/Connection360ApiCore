using Xunit;

// Estas pruebas ejecutan el Program.cs real del proceso ETL (que usa estado global del proceso:
// Environment.ExitCode, Console.CancelKeyPress y el DiagnosticListener de hosting), así que no
// pueden correr en paralelo entre sí.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
