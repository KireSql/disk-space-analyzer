using System.Globalization;
using DiskSpaceAnalyzer.Core.Cleanup;
using DiskSpaceAnalyzer.Core.Formatting;
using DiskSpaceAnalyzer.Core.Models;
using DiskSpaceAnalyzer.Desktop.ViewModels;
using DiskSpaceAnalyzer.Tests.Support;

namespace DiskSpaceAnalyzer.Tests.Desktop;

public class ScanSummaryViewModelTests
{
    private static readonly BinarySizeFormatter Formatter = new(CultureInfo.InvariantCulture);

    private static readonly ScanResult Result = new(
        new DirectoryNode("root", "/root", [new FileNode("a.bin", "/root/a.bin", 100)]),
        FilesScanned: 1,
        DirectoriesScanned: 1,
        InaccessibleDirectories: 0,
        Elapsed: TimeSpan.FromSeconds(1));

    private readonly FakeFileExplorerService _explorer = new();
    private readonly List<string> _revealed = [];

    private static CleanupFinding Finding(string name, long size)
        => new(new CleanupRule(name, name, $"desc {name}", $"/x/{name}"), new FileNode(name, $"/x/{name}", size));

    private ScanSummaryViewModel Summary(params CleanupFinding[] findings)
        => new(Result, Formatter, new CleanupReport(findings), _explorer, path =>
        {
            _revealed.Add(path);
            return Task.CompletedTask;
        });

    [Fact]
    public void WithoutFindings_NothingIsReclaimable()
    {
        var summary = Summary();

        Assert.False(summary.HasReclaimable);
        Assert.Empty(summary.Reclaimable);
    }

    [Fact]
    public void EmptySafeLocations_AreNotWorthAnAnnouncement()
        => Assert.False(Summary(Finding("Temp", 0)).HasReclaimable);

    [Fact]
    public void OneFinding_UsesTheSingularHeadline()
    {
        var summary = Summary(Finding("Temp", 2048));

        Assert.True(summary.HasReclaimable);
        Assert.Equal("2 KB", summary.ReclaimableSize);
        Assert.StartsWith("1 ubicación segura de limpiar", summary.ReclaimableHeadline);
        Assert.Equal("Temp", Assert.Single(summary.Reclaimable).Name);
    }

    [Fact]
    public void SeveralFindings_AreSummedAndListedFromLargestToSmallest()
    {
        var summary = Summary(Finding("Small", 1024), Finding("Big", 3072));

        Assert.Equal("4 KB", summary.ReclaimableSize);
        Assert.StartsWith("2 ubicaciones seguras de limpiar", summary.ReclaimableHeadline);
        Assert.Equal(["Big", "Small"], summary.Reclaimable.Select(item => item.Name));
        Assert.Equal(["3 KB", "1 KB"], summary.Reclaimable.Select(item => item.SizeText));
    }

    [Fact]
    public void EmptyFindings_DoNotCountTowardsTheHeadlineNorTheList()
    {
        var summary = Summary(Finding("Temp", 1024), Finding("Empty", 0));

        Assert.StartsWith("1 ubicación segura", summary.ReclaimableHeadline);
        Assert.Equal("Temp", Assert.Single(summary.Reclaimable).Name);
    }

    [Fact]
    public void EachItem_KeepsTheDescriptionAndPathForTheTooltipAndTheNavigation()
    {
        var item = Assert.Single(Summary(Finding("Temp", 1024)).Reclaimable);

        Assert.Equal("desc Temp", item.Description);
        Assert.Equal("/x/Temp", item.FullPath);
    }

    [Fact]
    public async Task OpenInApplication_AsksTheTreeToLocateThePath()
    {
        var item = Assert.Single(Summary(Finding("Temp", 1024)).Reclaimable);

        await item.OpenInApplicationCommand.ExecuteAsync(null);

        Assert.Equal(["/x/Temp"], _revealed);
        Assert.Empty(_explorer.OpenedPaths);
    }

    [Fact]
    public async Task OpenInExplorer_OpensThePathInTheFileExplorer()
    {
        var item = Assert.Single(Summary(Finding("Temp", 1024)).Reclaimable);

        await item.OpenInExplorerCommand.ExecuteAsync(null);

        Assert.Equal(["/x/Temp"], _explorer.OpenedPaths);
        Assert.Empty(_revealed);
    }
}
