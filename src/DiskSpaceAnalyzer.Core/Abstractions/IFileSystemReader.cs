namespace DiskSpaceAnalyzer.Core.Abstractions;

/// <summary>
/// Puerto de acceso al sistema de archivos. El núcleo depende de esta abstracción y no de
/// <c>System.IO</c> (inversión de dependencias), lo que permite probarlo sin tocar el disco.
/// </summary>
public interface IFileSystemReader
{
    bool DirectoryExists(string path);

    /// <summary>
    /// Enumera el contenido directo (no recursivo) de una carpeta.
    /// Puede lanzar <see cref="UnauthorizedAccessException"/> o <see cref="IOException"/>.
    /// </summary>
    IEnumerable<FileSystemEntryInfo> EnumerateEntries(string directoryPath);
}
