namespace DiskSpaceAnalyzer.Desktop.Services;

/// <summary>Permite al ViewModel pedir una carpeta al usuario sin conocer ningún control de UI.</summary>
public interface IFolderPickerService
{
    /// <returns>Ruta elegida, o <c>null</c> si el usuario canceló.</returns>
    Task<string?> PickFolderAsync();
}
