using System.Diagnostics;
using DiskSpaceAnalyzer.Core.Abstractions;
using DiskSpaceAnalyzer.Core.Models;

namespace DiskSpaceAnalyzer.Core.Scanning;

/// <summary>
/// Recorre el árbol de carpetas en un hilo de fondo y construye el modelo de dominio.
/// Solo conoce <see cref="IFileSystemReader"/>, por lo que es independiente del disco real.
/// </summary>
public sealed class DirectoryScanner : IDirectoryScanner
{
    /// <summary>Cada cuántos archivos se notifica el avance, para no saturar a quien escucha.</summary>
    private const int ProgressInterval = 500;

    private readonly IFileSystemReader _fileSystem;

    public DirectoryScanner(IFileSystemReader fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        _fileSystem = fileSystem;
    }

    public Task<ScanResult> ScanAsync(
        string rootPath,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        return Task.Run(() => Scan(rootPath, progress, cancellationToken), cancellationToken);
    }

    private ScanResult Scan(string rootPath, IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
    {
        if (!_fileSystem.DirectoryExists(rootPath))
        {
            throw new DirectoryNotFoundException($"No existe la carpeta '{rootPath}'.");
        }

        var startedAt = Stopwatch.GetTimestamp();
        var state = new ScanState(progress, cancellationToken);

        var root = ScanDirectory(GetDisplayName(rootPath), rootPath, state);
        state.ReportProgress(rootPath);

        return new ScanResult(
            root,
            state.FilesScanned,
            state.DirectoriesScanned,
            state.InaccessibleDirectories,
            Stopwatch.GetElapsedTime(startedAt));
    }

    private DirectoryNode ScanDirectory(string name, string path, ScanState state)
    {
        state.CancellationToken.ThrowIfCancellationRequested();
        state.DirectoriesScanned++;

        var children = new List<FileSystemNode>();
        var hasAccessErrors = false;

        try
        {
            foreach (var entry in _fileSystem.EnumerateEntries(path))
            {
                if (entry.IsLink)
                {
                    continue;
                }

                if (entry.IsDirectory)
                {
                    children.Add(ScanDirectory(entry.Name, entry.FullPath, state));
                }
                else
                {
                    children.Add(new FileNode(entry.Name, entry.FullPath, entry.SizeInBytes));
                    state.RecordFile(entry.SizeInBytes, path);
                }
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            // Una carpeta ilegible no debe abortar el análisis completo: se marca y se continúa.
            hasAccessErrors = true;
            state.InaccessibleDirectories++;
        }

        return new DirectoryNode(name, path, children, hasAccessErrors);
    }

    /// <summary>Nombre a mostrar: la última parte de la ruta o la ruta completa si es una raíz (p. ej. "D:\").</summary>
    private static string GetDisplayName(string path)
    {
        var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(path));
        return string.IsNullOrEmpty(name) ? path : name;
    }

    /// <summary>Contadores mutables de una ejecución; evita pasar muchos parámetros entre llamadas recursivas.</summary>
    private sealed class ScanState(IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
    {
        private long _bytesScanned;
        private int _filesSinceLastReport;

        public CancellationToken CancellationToken { get; } = cancellationToken;

        public long FilesScanned { get; private set; }

        public long DirectoriesScanned { get; set; }

        public int InaccessibleDirectories { get; set; }

        public void RecordFile(long sizeInBytes, string currentPath)
        {
            FilesScanned++;
            _bytesScanned += sizeInBytes;

            if (++_filesSinceLastReport >= ProgressInterval)
            {
                ReportProgress(currentPath);
            }
        }

        public void ReportProgress(string currentPath)
        {
            _filesSinceLastReport = 0;
            progress?.Report(new ScanProgress(FilesScanned, DirectoriesScanned, _bytesScanned, currentPath));
        }
    }
}
