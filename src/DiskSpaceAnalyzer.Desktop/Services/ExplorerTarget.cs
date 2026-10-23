namespace DiskSpaceAnalyzer.Desktop.Services;

/// <summary>Decide qué carpeta debe abrir el explorador para una ruta dada. Aislado para poder probarlo.</summary>
public static class ExplorerTarget
{
    /// <returns>La propia carpeta, la carpeta contenedora si es un archivo, o <c>null</c> si no existe.</returns>
    public static string? Resolve(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (Directory.Exists(path))
        {
            return path;
        }

        var parent = Path.GetDirectoryName(path);
        return parent is not null && Directory.Exists(parent) && File.Exists(path) ? parent : null;
    }
}
