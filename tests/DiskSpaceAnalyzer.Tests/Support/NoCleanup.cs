using DiskSpaceAnalyzer.Core.Cleanup;

namespace DiskSpaceAnalyzer.Tests.Support;

/// <summary>Clasificador sin reglas: para las pruebas que no tratan de las ubicaciones seguras de limpiar.</summary>
public static class NoCleanup
{
    public static CleanupAdvisor Advisor { get; } = new([], StringComparison.Ordinal);
}
