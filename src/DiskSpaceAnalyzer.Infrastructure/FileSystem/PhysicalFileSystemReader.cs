using DiskSpaceAnalyzer.Core.Abstractions;

namespace DiskSpaceAnalyzer.Infrastructure.FileSystem;

/// <summary>Adaptador de <see cref="IFileSystemReader"/> sobre el disco real mediante <c>System.IO</c>.</summary>
public sealed class PhysicalFileSystemReader : IFileSystemReader
{
    private static readonly EnumerationOptions Options = new()
    {
        AttributesToSkip = 0, // incluir ocultos y de sistema: también ocupan espacio
        IgnoreInaccessible = false, // los errores de acceso los gestiona el escáner
        RecurseSubdirectories = false,
        ReturnSpecialDirectories = false,
    };

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public IEnumerable<FileSystemEntryInfo> EnumerateEntries(string directoryPath)
    {
        var directory = new DirectoryInfo(directoryPath);

        foreach (var info in directory.EnumerateFileSystemInfos("*", Options))
        {
            var length = info is FileInfo file ? file.Length : 0;

            yield return new FileSystemEntryInfo(
                info.Name,
                info.FullName,
                IsDirectory: info is DirectoryInfo,
                length,
                IsLink: IsLink(info));
        }
    }

    private static bool IsLink(FileSystemInfo info)
    {
        if ((info.Attributes & FileAttributes.ReparsePoint) == 0)
        {
            return false;
        }

        try
        {
            // Los marcadores de OneDrive también son "reparse points" pero no son enlaces: LinkTarget es null.
            return info.LinkTarget is not null;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            // Ante la duda se trata como enlace: es preferible omitirlo a arriesgar un bucle infinito.
            return true;
        }
    }
}
