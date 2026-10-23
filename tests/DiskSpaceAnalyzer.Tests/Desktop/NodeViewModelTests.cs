using System.Globalization;
using DiskSpaceAnalyzer.Core.Formatting;
using DiskSpaceAnalyzer.Core.Models;
using DiskSpaceAnalyzer.Desktop.ViewModels;
using DiskSpaceAnalyzer.Tests.Support;

namespace DiskSpaceAnalyzer.Tests.Desktop;

public class NodeViewModelTests
{
    private readonly FakeFileExplorerService _explorer = new();
    private readonly NodeViewModelFactory _factory;

    public NodeViewModelTests()
        => _factory = new NodeViewModelFactory(new BinarySizeFormatter(CultureInfo.InvariantCulture), _explorer);

    private static DirectoryNode SampleTree() => new(
        "root",
        "/root",
        [
            new FileNode("small.txt", "/root/small.txt", 100),
            new DirectoryNode("big", "/root/big", [new FileNode("huge.bin", "/root/big/huge.bin", 900)]),
        ]);

    [Fact]
    public void Children_AreOrderedBySizeDescending()
    {
        var root = _factory.Create(SampleTree(), 1000);

        Assert.Equal(["big", "small.txt"], root.Children.Select(c => c.Name));
    }

    [Fact]
    public void Percent_IsRelativeToTheParent()
    {
        var tree = SampleTree();
        var root = _factory.Create(tree, tree.SizeInBytes);

        Assert.Equal(100, root.Percent);
        Assert.Equal(90, root.Children[0].Percent);
        Assert.Equal(10, root.Children[1].Percent);
    }

    [Fact]
    public void Percent_IsZeroWhenTheParentIsEmpty()
        => Assert.Equal(0, _factory.Create(new FileNode("f", "/f", 0), 0).Percent);

    [Fact]
    public void SizeText_UsesTheFormatter()
        => Assert.Equal("1 KB", _factory.Create(new FileNode("f", "/f", 1024), 1024).SizeText);

    [Fact]
    public void Children_AreCreatedOnlyOnce()
    {
        var root = _factory.Create(SampleTree(), 1000);

        Assert.Same(root.Children, root.Children);
    }

    [Fact]
    public void Files_HaveNoChildrenAndAreNotDirectories()
    {
        var file = _factory.Create(new FileNode("f", "/f", 1), 1);

        Assert.False(file.IsDirectory);
        Assert.Empty(file.Children);
    }

    [Fact]
    public async Task OpenInExplorerCommand_AsksTheServiceToOpenTheNodePath()
    {
        var folder = _factory.Create(SampleTree(), 1000).Children[0];

        await folder.OpenInExplorerCommand.ExecuteAsync(null);

        Assert.Equal(["/root/big"], _explorer.OpenedPaths);
    }

    [Fact]
    public async Task OpenInExplorerCommand_PassesTheFilePathAndLeavesParentResolutionToTheService()
    {
        var file = _factory.Create(SampleTree(), 1000).Children[1];

        await file.OpenInExplorerCommand.ExecuteAsync(null);

        Assert.Equal(["/root/small.txt"], _explorer.OpenedPaths);
    }

    [Fact]
    public void IsExpanded_RaisesPropertyChanged()
    {
        var node = _factory.Create(SampleTree(), 1000);
        var raised = new List<string?>();
        node.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        node.IsExpanded = true;

        Assert.Contains(nameof(NodeViewModel.IsExpanded), raised);
    }
}
