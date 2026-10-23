namespace DiskSpaceAnalyzer.Core.Abstractions;

/// <summary>Datos de un elemento leído del sistema de archivos.</summary>
/// <param name="Name">Nombre sin ruta.</param>
/// <param name="FullPath">Ruta completa.</param>
/// <param name="IsDirectory">Verdadero si es una carpeta.</param>
/// <param name="SizeInBytes">Tamaño del archivo; 0 para carpetas.</param>
/// <param name="IsLink">Enlace simbólico o punto de unión. Se omite al analizar para evitar bucles y doble conteo.</param>
public readonly record struct FileSystemEntryInfo(
    string Name,
    string FullPath,
    bool IsDirectory,
    long SizeInBytes,
    bool IsLink);
