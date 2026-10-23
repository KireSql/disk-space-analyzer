using DiskSpaceAnalyzer.Core.Cleanup;
using DiskSpaceAnalyzer.Core.Formatting;
using DiskSpaceAnalyzer.Core.Models;

namespace DiskSpaceAnalyzer.Desktop.ViewModels;

/// <summary>Cifras de un análisis ya formateadas para mostrarse en las tarjetas de resumen.</summary>
public sealed class ScanSummaryViewModel
{
    public ScanSummaryViewModel(ScanResult result, ISizeFormatter sizeFormatter, CleanupReport cleanupReport)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(sizeFormatter);
        ArgumentNullException.ThrowIfNull(cleanupReport);

        TotalSize = sizeFormatter.Format(result.Root.SizeInBytes);
        Files = result.FilesScanned.ToString("N0");
        Folders = result.DirectoriesScanned.ToString("N0");
        Elapsed = $"{result.Elapsed.TotalSeconds:0.0} s";
        InaccessibleDirectories = result.InaccessibleDirectories;

        // Una ubicación segura pero vacía no aporta nada que avisar.
        var reclaimable = cleanupReport.Findings
            .Where(finding => finding.Node.SizeInBytes > 0)
            .OrderByDescending(finding => finding.Node.SizeInBytes)
            .ToArray();

        HasReclaimable = reclaimable.Length > 0;
        ReclaimableSize = sizeFormatter.Format(reclaimable.Sum(finding => finding.Node.SizeInBytes));
        ReclaimableHeadline = reclaimable.Length == 1
            ? "1 ubicación segura de limpiar, marcada en verde en el árbol"
            : $"{reclaimable.Length} ubicaciones seguras de limpiar, marcadas en verde en el árbol";
        ReclaimableDetails = string.Join(
            " · ",
            reclaimable.Select(finding => $"{finding.Rule.Name} ({sizeFormatter.Format(finding.Node.SizeInBytes)})"));
    }

    public string TotalSize { get; }

    public string Files { get; }

    public string Folders { get; }

    public string Elapsed { get; }

    public int InaccessibleDirectories { get; }

    /// <summary>Hay ubicaciones seguras de limpiar con contenido dentro del análisis.</summary>
    public bool HasReclaimable { get; }

    /// <summary>Espacio total que ocupan, ya sumado sin solapes.</summary>
    public string ReclaimableSize { get; }

    public string ReclaimableHeadline { get; }

    /// <summary>Desglose "Nombre (tamaño) · Nombre (tamaño)", de mayor a menor.</summary>
    public string ReclaimableDetails { get; }
}
