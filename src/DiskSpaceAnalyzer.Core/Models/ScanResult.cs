namespace DiskSpaceAnalyzer.Core.Models;

/// <summary>Resultado de un análisis completo.</summary>
/// <param name="Root">Árbol de carpetas y archivos analizados.</param>
/// <param name="FilesScanned">Cantidad de archivos contabilizados.</param>
/// <param name="DirectoriesScanned">Cantidad de carpetas recorridas (incluida la raíz).</param>
/// <param name="InaccessibleDirectories">Carpetas que no se pudieron leer por completo.</param>
/// <param name="Elapsed">Duración del análisis.</param>
public sealed record ScanResult(
    DirectoryNode Root,
    long FilesScanned,
    long DirectoriesScanned,
    int InaccessibleDirectories,
    TimeSpan Elapsed);
