using DiskSpaceAnalyzer.Core.Models;

namespace DiskSpaceAnalyzer.Tests.Core;

public class DirectoryNodeTests
{
    [Fact]
    public void SizeInBytes_IsTheSumOfAllDescendants()
    {
        var inner = new DirectoryNode("inner", "/root/inner", [new FileNode("b.txt", "/root/inner/b.txt", 30)]);
        var root = new DirectoryNode("root", "/root", [new FileNode("a.txt", "/root/a.txt", 10), inner]);

        Assert.Equal(40, root.SizeInBytes);
        Assert.Equal(30, inner.SizeInBytes);
    }

    [Fact]
    public void EmptyDirectory_HasZeroSize()
    {
        var node = new DirectoryNode("empty", "/empty", []);

        Assert.Equal(0, node.SizeInBytes);
        Assert.Empty(node.Children);
    }

    [Fact]
    public void Children_AreSnapshottedAtConstruction()
    {
        var source = new List<FileSystemNode> { new FileNode("a", "/a", 5) };
        var node = new DirectoryNode("root", "/", source);

        source.Add(new FileNode("b", "/b", 100));

        Assert.Single(node.Children);
        Assert.Equal(5, node.SizeInBytes);
    }

    [Fact]
    public void Kinds_AreReportedPolymorphically()
    {
        Assert.Equal(NodeKind.Directory, new DirectoryNode("d", "/d", []).Kind);
        Assert.Equal(NodeKind.File, new FileNode("f", "/f", 1).Kind);
    }

    [Fact]
    public void FileNode_RejectsNegativeSize()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new FileNode("f", "/f", -1));
}
