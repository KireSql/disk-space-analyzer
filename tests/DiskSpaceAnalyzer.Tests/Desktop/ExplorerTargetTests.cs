using DiskSpaceAnalyzer.Desktop.Services;

namespace DiskSpaceAnalyzer.Tests.Desktop;

/// <summary>Pruebas contra una carpeta temporal real: la decisión depende de lo que haya en disco.</summary>
public sealed class ExplorerTargetTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("dsa-explorer-").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void Resolve_ReturnsTheFolderItself()
        => Assert.Equal(_root, ExplorerTarget.Resolve(_root));

    [Fact]
    public void Resolve_ReturnsTheContainingFolderForAFile()
    {
        var file = Path.Combine(_root, "a.txt");
        File.WriteAllText(file, "x");

        Assert.Equal(_root, ExplorerTarget.Resolve(file));
    }

    [Fact]
    public void Resolve_ReturnsNullWhenThePathNoLongerExists()
        => Assert.Null(ExplorerTarget.Resolve(Path.Combine(_root, "deleted-after-scan.txt")));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_ReturnsNullForBlankPaths(string path)
        => Assert.Null(ExplorerTarget.Resolve(path));
}
