using DiskSpaceAnalyzer.Core.Abstractions;

namespace DiskSpaceAnalyzer.Tests.Support;

/// <summary>Sistema de archivos en memoria para probar el escáner sin tocar el disco.</summary>
public sealed class FakeFileSystemReader : IFileSystemReader
{
    private readonly Dictionary<string, List<FileSystemEntryInfo>> _directories = new(StringComparer.Ordinal);
    private readonly HashSet<string> _inaccessible = new(StringComparer.Ordinal);

    public FakeFileSystemReader AddDirectory(string path)
    {
        _directories.TryAdd(path, []);
        return this;
    }

    public FakeFileSystemReader AddSubdirectory(string parent, string name)
    {
        var path = Combine(parent, name);
        AddDirectory(parent);
        AddDirectory(path);
        _directories[parent].Add(new FileSystemEntryInfo(name, path, IsDirectory: true, 0, IsLink: false));
        return this;
    }

    public FakeFileSystemReader AddFile(string parent, string name, long size)
    {
        AddDirectory(parent);
        _directories[parent].Add(new FileSystemEntryInfo(name, Combine(parent, name), IsDirectory: false, size, IsLink: false));
        return this;
    }

    public FakeFileSystemReader AddLink(string parent, string name)
    {
        AddDirectory(parent);
        _directories[parent].Add(new FileSystemEntryInfo(name, Combine(parent, name), IsDirectory: true, 0, IsLink: true));
        return this;
    }

    public FakeFileSystemReader MarkInaccessible(string path)
    {
        _inaccessible.Add(path);
        return this;
    }

    public bool DirectoryExists(string path) => _directories.ContainsKey(path);

    public IEnumerable<FileSystemEntryInfo> EnumerateEntries(string directoryPath)
    {
        if (_inaccessible.Contains(directoryPath))
        {
            throw new UnauthorizedAccessException($"Acceso denegado a '{directoryPath}'.");
        }

        return _directories[directoryPath];
    }

    private static string Combine(string parent, string name) => $"{parent}/{name}";
}
