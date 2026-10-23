using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace DiskSpaceAnalyzer.Desktop.Services;

/// <summary>Implementación de <see cref="IFolderPickerService"/> con el selector nativo de Avalonia.</summary>
public sealed class AvaloniaFolderPickerService : IFolderPickerService
{
    private readonly Func<TopLevel?> _topLevelAccessor;

    public AvaloniaFolderPickerService(Func<TopLevel?> topLevelAccessor)
    {
        ArgumentNullException.ThrowIfNull(topLevelAccessor);
        _topLevelAccessor = topLevelAccessor;
    }

    public async Task<string?> PickFolderAsync()
    {
        var topLevel = _topLevelAccessor();
        if (topLevel is null)
        {
            return null;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Selecciona la carpeta a analizar",
            AllowMultiple = false,
        });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }
}
