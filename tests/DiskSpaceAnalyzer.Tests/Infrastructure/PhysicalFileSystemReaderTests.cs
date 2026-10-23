using DiskSpaceAnalyzer.Core.Scanning;
using DiskSpaceAnalyzer.Infrastructure.FileSystem;

namespace DiskSpaceAnalyzer.Tests.Infrastructure;

/// <summary>Pruebas de integración contra una carpeta temporal real.</summary>
public sealed class PhysicalFileSystemReaderTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("dsa-tests-").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void EnumerateEntries_ListsFilesAndDirectoriesWithTheirSize()
    {
        File.WriteAllBytes(Path.Combine(_root, "a.bin"), new byte[123]);
        Directory.CreateDirectory(Path.Combine(_root, "sub"));

        var entries = new PhysicalFileSystemReader().EnumerateEntries(_root).ToList();

        var file = Assert.Single(entries, e => !e.IsDirectory);
        Assert.Equal("a.bin", file.Name);
        Assert.Equal(123, file.SizeInBytes);
        Assert.Single(entries, e => e.IsDirectory && e.Name == "sub");
        Assert.All(entries, e => Assert.False(e.IsLink));
    }

    [Fact]
    public void EnumerateEntries_IncludesHiddenFiles()
    {
        var path = Path.Combine(_root, "hidden.bin");
        File.WriteAllBytes(path, new byte[10]);
        File.SetAttributes(path, FileAttributes.Hidden);

        var entries = new PhysicalFileSystemReader().EnumerateEntries(_root);

        Assert.Contains(entries, e => e.Name == "hidden.bin");
    }

    [Fact]
    public async Task Scanner_WithThePhysicalReader_ComputesTheRealTotal()
    {
        File.WriteAllBytes(Path.Combine(_root, "a.bin"), new byte[1000]);
        var nested = Directory.CreateDirectory(Path.Combine(_root, "x", "y")).FullName;
        File.WriteAllBytes(Path.Combine(nested, "b.bin"), new byte[2500]);

        var result = await new DirectoryScanner(new PhysicalFileSystemReader()).ScanAsync(_root);

        Assert.Equal(3500, result.Root.SizeInBytes);
        Assert.Equal(2, result.FilesScanned);
        Assert.Equal(3, result.DirectoriesScanned);
    }

    [Fact]
    public void DirectoryExists_ReflectsTheDisk()
    {
        var reader = new PhysicalFileSystemReader();

        Assert.True(reader.DirectoryExists(_root));
        Assert.False(reader.DirectoryExists(Path.Combine(_root, "nope")));
    }
}
