using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace DiskSpaceAnalyzer.Desktop.Services;

/// <summary>
/// Implementación de <see cref="IFileExplorerService"/> con el lanzador de Avalonia, que abre el
/// gestor de archivos propio de cada sistema (Explorador en Windows, Finder en macOS, etc.).
/// </summary>
public sealed class AvaloniaFileExplorerService : IFileExplorerService
{
    private readonly Func<TopLevel?> _topLevelAccessor;

    public AvaloniaFileExplorerService(Func<TopLevel?> topLevelAccessor)
    {
        ArgumentNullException.ThrowIfNull(topLevelAccessor);
        _topLevelAccessor = topLevelAccessor;
    }

    public async Task OpenAsync(string path)
    {
        var topLevel = _topLevelAccessor();
        var target = ExplorerTarget.Resolve(path);

        if (topLevel is null || target is null)
        {
            return;
        }

        await topLevel.Launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(target));
    }
}
