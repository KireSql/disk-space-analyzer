namespace DiskSpaceAnalyzer.Core.Cleanup;

/// <summary>Ubicaciones seguras de limpiar encontradas en un análisis y espacio que ocupan.</summary>
public sealed record CleanupReport(IReadOnlyList<CleanupFinding> Findings)
{
    public static CleanupReport Empty { get; } = new([]);

    /// <summary>Suma de los hallazgos. Ya no se solapan: <see cref="ICleanupAnalyzer"/> solo devuelve los más externos.</summary>
    public long ReclaimableBytes => Findings.Sum(finding => finding.Node.SizeInBytes);
}
