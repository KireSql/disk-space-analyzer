namespace DiskSpaceAnalyzer.Desktop.Services;

/// <summary>Permite abrir el explorador de archivos del sistema sin que el ViewModel conozca la plataforma.</summary>
public interface IFileExplorerService
{
    /// <summary>
    /// Abre la carpeta indicada o, si es un archivo, la carpeta que lo contiene.
    /// No hace nada si la ruta ya no existe (p. ej. se borró después del análisis).
    /// </summary>
    Task OpenAsync(string path);
}
