using DiskSpaceAnalyzer.Core.Formatting;
using DiskSpaceAnalyzer.Core.Models;

namespace DiskSpaceAnalyzer.Desktop.ViewModels;

/// <summary>Cifras de un análisis ya formateadas para mostrarse en las tarjetas de resumen.</summary>
public sealed class ScanSummaryViewModel
{
    public ScanSummaryViewModel(ScanResult result, ISizeFormatter sizeFormatter)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(sizeFormatter);

        TotalSize = sizeFormatter.Format(result.Root.SizeInBytes);
        Files = result.FilesScanned.ToString("N0");
        Folders = result.DirectoriesScanned.ToString("N0");
        Elapsed = $"{result.Elapsed.TotalSeconds:0.0} s";
        InaccessibleDirectories = result.InaccessibleDirectories;
    }

    public string TotalSize { get; }

    public string Files { get; }

    public string Folders { get; }

    public string Elapsed { get; }

    public int InaccessibleDirectories { get; }
}
