using DiskSpaceAnalyzer.Core.Models;
using DiskSpaceAnalyzer.Core.Scanning;

namespace DiskSpaceAnalyzer.Core.Abstractions;

/// <summary>Analiza una carpeta y devuelve el árbol con el tamaño de cada elemento.</summary>
public interface IDirectoryScanner
{
    /// <exception cref="DirectoryNotFoundException">La carpeta raíz no existe.</exception>
    /// <exception cref="OperationCanceledException">El análisis fue cancelado.</exception>
    Task<ScanResult> ScanAsync(
        string rootPath,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
