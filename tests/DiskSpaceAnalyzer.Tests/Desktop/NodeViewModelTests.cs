using System.Globalization;
using DiskSpaceAnalyzer.Core.Formatting;
using DiskSpaceAnalyzer.Core.Models;
using DiskSpaceAnalyzer.Desktop.ViewModels;
using DiskSpaceAnalyzer.Tests.Support;

namespace DiskSpaceAnalyzer.Tests.Desktop;

public class NodeViewModelTests
{
    private readonly FakeFileExplorerService _explorer = new();
    private readonly FakeUiYield _yield = new(manual: true);
    private readonly NodeViewModelFactory _factory;

    public NodeViewModelTests()
        => _factory = new NodeViewModelFactory(
            new BinarySizeFormatter(CultureInfo.InvariantCulture), _explorer, NoCleanup.Advisor, _yield);

    private static DirectoryNode SampleTree() => new(
        "root",
        "/root",
        [
            new FileNode("small.txt", "/root/small.txt", 100),
            new DirectoryNode("big", "/root/big", [new FileNode("huge.bin", "/root/big/huge.bin", 900)]),
        ]);

    /// <summary>Carpeta con <paramref name="count"/> archivos; el archivo i pesa i+1 bytes.</summary>
    private static DirectoryNode WideFolder(int count) => new(
        "wide",
        "/wide",
        Enumerable.Range(0, count).Select(i => (FileSystemNode)new FileNode($"f{i}.bin", $"/wide/f{i}.bin", i + 1)));

    private async Task<NodeViewModel> LoadedRoot(FileSystemNode node, long? parentSize = null)
    {
        var root = _factory.Create(node, parentSize ?? node.SizeInBytes);
        await root.LoadChildrenAsync();
        return root;
    }

    // ---- Datos básicos ----

    [Fact]
    public async Task Children_AreOrderedBySizeDescending()
    {
        var root = await LoadedRoot(SampleTree(), 1000);

        Assert.Equal(["big", "small.txt"], root.Children.Select(c => c.Name));
    }

    [Fact]
    public async Task Percent_IsRelativeToTheParent()
    {
        var root = await LoadedRoot(SampleTree());

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
    public void Files_HaveNoChildrenAndAreNotDirectories()
    {
        var file = _factory.Create(new FileNode("f", "/f", 1), 1);

        Assert.False(file.IsDirectory);
        Assert.True(file.IsFile);
        Assert.Empty(file.Children);
    }

    [Fact]
    public async Task OpenInExplorerCommand_AsksTheServiceToOpenTheNodePath()
    {
        var folder = (await LoadedRoot(SampleTree(), 1000)).Children[0];

        await folder.OpenInExplorerCommand.ExecuteAsync(null);

        Assert.Equal(["/root/big"], _explorer.OpenedPaths);
    }

    [Fact]
    public async Task OpenInExplorerCommand_PassesTheFilePathAndLeavesParentResolutionToTheService()
    {
        var file = (await LoadedRoot(SampleTree(), 1000)).Children[1];

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

    // ---- Carga por tandas ----

    [Fact]
    public void AFolderWithContent_StartsWithOnlyTheLoadingRow()
    {
        var folder = _factory.Create(SampleTree(), 1000);

        var row = Assert.Single(folder.Children);
        Assert.True(row.IsPlaceholder);
        Assert.False(row.IsFile);
        Assert.False(row.IsDirectory);
    }

    [Fact]
    public void AnEmptyFolder_HasNoRowsAtAll()
        => Assert.Empty(_factory.Create(new DirectoryNode("empty", "/empty", []), 0).Children);

    [Fact]
    public async Task OnceLoaded_TheLoadingRowIsGoneAndNoSpinnerIsShown()
    {
        var root = await LoadedRoot(SampleTree());

        Assert.All(root.Children, child => Assert.False(child.IsPlaceholder));
        Assert.False(root.IsLoadingChildren);
    }

    [Fact]
    public async Task ExpandingAFolder_LoadsItsChildren()
    {
        var root = _factory.Create(SampleTree(), 1000);

        root.IsExpanded = true;
        await root.LoadChildrenAsync();

        Assert.Equal(2, root.Children.Count);
    }

    [Fact]
    public async Task LoadingTwice_DoesNotDuplicateRows()
    {
        var root = _factory.Create(SampleTree(), 1000);
        var children = root.Children;

        await root.LoadChildrenAsync();
        await root.LoadChildrenAsync();

        Assert.Equal(2, root.Children.Count);
        Assert.Same(children, root.Children);
    }

    [Fact]
    public void ASmallFolder_FinishesLoadingSynchronously()
    {
        var folder = _factory.Create(WideFolder(10), 55);

        var loading = folder.LoadChildrenAsync();

        Assert.True(loading.IsCompleted);
        Assert.Equal(10, folder.Children.Count);
    }

    [Fact]
    public async Task ABigFolder_LoadsInBatchesWhileShowingProgress()
    {
        var folder = _factory.Create(WideFolder(120), 7260);

        var loading = folder.LoadChildrenAsync();

        // Solo la primera tanda ha llegado: el resto se publica cediendo el turno a la ventana entre tandas.
        Assert.False(loading.IsCompleted);
        Assert.Equal(1, _yield.Yields);
        Assert.True(folder.IsLoadingChildren);
        Assert.True(folder.Children[^1].IsPlaceholder);
        Assert.Equal("Cargando… 25 de 120", folder.Children[^1].LoadingText);
        Assert.Equal("25 de 120", folder.LoadingProgress);
        Assert.Equal(26, folder.Children.Count);

        _yield.Release();
        Assert.Equal(51, folder.Children.Count);
        Assert.Equal("Cargando… 50 de 120", folder.Children[^1].LoadingText);

        _yield.Release();
        _yield.Release();
        Assert.False(loading.IsCompleted);

        _yield.Release(); // la última tanda (20 filas) ya no cede: termina aquí
        await loading;

        Assert.Equal(4, _yield.Yields);
        Assert.False(folder.IsLoadingChildren);
        Assert.Equal(string.Empty, folder.LoadingProgress);
        Assert.Equal(120, folder.Children.Count);
        Assert.All(folder.Children, child => Assert.False(child.IsPlaceholder));
        Assert.Equal("f119.bin", folder.Children[0].Name);
        Assert.Equal("f0.bin", folder.Children[^1].Name);
    }

    // ---- Localizar una ruta ----

    private static DirectoryNode NestedTree() => new(
        "root",
        "/root",
        [
            new DirectoryNode(
                "a",
                "/root/a",
                [new DirectoryNode("b", "/root/a/b", [new FileNode("c.bin", "/root/a/b/c.bin", 50)])]),
            new DirectoryNode("a2", "/root/a2", [new FileNode("d.bin", "/root/a2/d.bin", 10)]),
        ]);

    [Fact]
    public async Task Reveal_ExpandsEveryFolderOnTheWayAndReturnsTheTarget()
    {
        var root = _factory.Create(NestedTree(), 60);

        var target = await root.RevealAsync("/root/a/b", StringComparison.Ordinal);

        Assert.Equal("/root/a/b", target?.FullPath);
        Assert.True(root.IsExpanded);
        Assert.True(root.Children.Single(c => c.Name == "a").IsExpanded);
    }

    [Fact]
    public async Task Reveal_OfTheNodeItself_ReturnsIt()
    {
        var root = _factory.Create(NestedTree(), 60);

        Assert.Same(root, await root.RevealAsync("/root", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Reveal_IgnoresCaseOnlyWhenTheComparisonSaysSo()
    {
        var root = _factory.Create(NestedTree(), 60);

        Assert.Null(await root.RevealAsync("/ROOT/A/B", StringComparison.Ordinal));
        Assert.Equal("/root/a/b", (await root.RevealAsync("/ROOT/A/B", StringComparison.OrdinalIgnoreCase))?.FullPath);
    }

    [Fact]
    public async Task Reveal_ReturnsNullForAPathOutsideTheTree()
    {
        var root = _factory.Create(NestedTree(), 60);

        Assert.Null(await root.RevealAsync("/elsewhere/x", StringComparison.Ordinal));
        Assert.Null(await root.RevealAsync("/root/missing", StringComparison.Ordinal));
        Assert.False(root.IsExpanded);
    }

    [Fact]
    public async Task Reveal_DoesNotMistakeASiblingWithTheSamePrefixForAnAncestor()
    {
        var root = _factory.Create(NestedTree(), 60);

        var target = await root.RevealAsync("/root/a2", StringComparison.Ordinal);

        Assert.Equal("/root/a2", target?.FullPath);
        Assert.False(root.Children.Single(c => c.Name == "a").IsExpanded);
    }
}
