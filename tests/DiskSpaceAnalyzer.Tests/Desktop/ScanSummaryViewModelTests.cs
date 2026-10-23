using System.Globalization;
using DiskSpaceAnalyzer.Core.Cleanup;
using DiskSpaceAnalyzer.Core.Formatting;
using DiskSpaceAnalyzer.Core.Models;
using DiskSpaceAnalyzer.Desktop.ViewModels;

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

    private static CleanupFinding Finding(string name, long size)
        => new(new CleanupRule(name, name, "desc", $"/x/{name}"), new FileNode(name, $"/x/{name}", size));

    private static ScanSummaryViewModel Summary(params CleanupFinding[] findings)
        => new(Result, Formatter, new CleanupReport(findings));

    [Fact]
    public void WithoutFindings_NothingIsReclaimable()
        => Assert.False(Summary().HasReclaimable);

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
        Assert.Equal("Temp (2 KB)", summary.ReclaimableDetails);
    }

    [Fact]
    public void SeveralFindings_AreSummedAndListedFromLargestToSmallest()
    {
        var summary = Summary(Finding("Small", 1024), Finding("Big", 3072));

        Assert.Equal("4 KB", summary.ReclaimableSize);
        Assert.StartsWith("2 ubicaciones seguras de limpiar", summary.ReclaimableHeadline);
        Assert.Equal("Big (3 KB) · Small (1 KB)", summary.ReclaimableDetails);
    }

    [Fact]
    public void EmptyFindings_DoNotCountTowardsTheHeadline()
    {
        var summary = Summary(Finding("Temp", 1024), Finding("Empty", 0));

        Assert.StartsWith("1 ubicación segura", summary.ReclaimableHeadline);
        Assert.Equal("Temp (1 KB)", summary.ReclaimableDetails);
    }
}
