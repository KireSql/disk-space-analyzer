namespace DiskSpaceAnalyzer.Core.Scanning;

/// <summary>Avance parcial de un análisis en curso.</summary>
public readonly record struct ScanProgress(
    long FilesScanned,
    long DirectoriesScanned,
    long BytesScanned,
    string CurrentPath);
