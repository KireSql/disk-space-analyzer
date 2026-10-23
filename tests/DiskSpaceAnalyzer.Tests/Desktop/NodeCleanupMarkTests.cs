using System.Globalization;
using DiskSpaceAnalyzer.Core.Cleanup;
using DiskSpaceAnalyzer.Core.Formatting;
using DiskSpaceAnalyzer.Core.Models;
using DiskSpaceAnalyzer.Desktop.ViewModels;
using DiskSpaceAnalyzer.Tests.Support;

namespace DiskSpaceAnalyzer.Tests.Desktop;

/// <summary>Cómo presenta un nodo las ubicaciones seguras de limpiar.</summary>
public class NodeCleanupMarkTests
{
    private static readonly string Base = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "dsa-node-cleanup"));
    private static readonly string Temp = Path.Combine(Base, "Local", "Temp");

    private readonly NodeViewModelFactory _factory = new(
        new BinarySizeFormatter(CultureInfo.InvariantCulture),
        new FakeFileExplorerService(),
        new CleanupAdvisor(
            [new CleanupRule("temp", "Archivos temporales", "Se vuelven a crear solos.", Temp)],
            StringComparison.Ordinal));

    private static DirectoryNode Dir(string path, params FileSystemNode[] children)
        => new(Path.GetFileName(path), path, children);

    [Fact]
    public void TheSafeLocation_GetsTheBadgeFlagsAndATooltipWithItsDescription()
    {
        var node = _factory.Create(Dir(Temp), 100);

        Assert.True(node.IsCleanupMarked);
        Assert.True(node.IsCleanupRoot);
        Assert.True(node.IsCleanupDirectory);
        Assert.False(node.IsPlainDirectory);
        Assert.StartsWith("Seguro de limpiar: Archivos temporales", node.CleanupTooltip);
        Assert.Contains("Se vuelven a crear solos.", node.CleanupTooltip);
    }

    [Fact]
    public void AFolderInsideTheSafeLocation_IsMarkedButDoesNotGetTheBadge()
    {
        var node = _factory.Create(Dir(Path.Combine(Temp, "cache")), 100);

        Assert.True(node.IsCleanupMarked);
        Assert.False(node.IsCleanupRoot);
        Assert.True(node.IsCleanupDirectory);
        Assert.StartsWith("Dentro de una ubicación segura de limpiar", node.CleanupTooltip);
    }

    [Fact]
    public void AnUnrelatedFolder_HasNoMarkAndNoTooltip()
    {
        var node = _factory.Create(Dir(Path.Combine(Base, "Documents")), 100);

        Assert.False(node.IsCleanupMarked);
        Assert.False(node.IsCleanupRoot);
        Assert.False(node.IsCleanupDirectory);
        Assert.True(node.IsPlainDirectory);
        Assert.Null(node.CleanupTooltip);
    }

    [Fact]
    public void AFolderNamedTempElsewhere_IsNotMarked()
    {
        var node = _factory.Create(Dir(Path.Combine(Base, "MyProject", "Temp")), 100);

        Assert.False(node.IsCleanupMarked);
    }

    [Fact]
    public void ChildrenOfTheSafeLocation_AreMarkedWhenTheyAreCreated()
    {
        var tree = Dir(Temp, Dir(Path.Combine(Temp, "cache"), new FileNode("a.bin", Path.Combine(Temp, "cache", "a.bin"), 10)));

        var cache = _factory.Create(tree, 10).Children[0];

        Assert.True(cache.IsCleanupMarked);
        Assert.False(cache.IsCleanupRoot);
    }

    [Fact]
    public void Files_AreNeverShownAsCleanupFolders()
    {
        var file = _factory.Create(new FileNode("a.bin", Path.Combine(Temp, "a.bin"), 10), 10);

        Assert.True(file.IsCleanupMarked);
        Assert.False(file.IsCleanupDirectory);
        Assert.False(file.IsPlainDirectory);
    }
}
