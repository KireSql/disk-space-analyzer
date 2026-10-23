using DiskSpaceAnalyzer.Core.Scanning;
using DiskSpaceAnalyzer.Tests.Support;

namespace DiskSpaceAnalyzer.Tests.Core;

public class DirectoryScannerTests
{
    [Fact]
    public async Task ScanAsync_SumsSizesRecursively()
    {
        var fs = new FakeFileSystemReader()
            .AddFile("/root", "a.txt", 100)
            .AddSubdirectory("/root", "docs")
            .AddFile("/root/docs", "b.txt", 200)
            .AddFile("/root/docs", "c.txt", 300);

        var result = await new DirectoryScanner(fs).ScanAsync("/root");

        Assert.Equal(600, result.Root.SizeInBytes);
        Assert.Equal(3, result.FilesScanned);
        Assert.Equal(2, result.DirectoriesScanned);
        Assert.Equal(0, result.InaccessibleDirectories);
    }

    [Fact]
    public async Task ScanAsync_BuildsTheTreeWithNamesAndPaths()
    {
        var fs = new FakeFileSystemReader()
            .AddFile("/root", "a.txt", 1)
            .AddSubdirectory("/root", "docs");

        var result = await new DirectoryScanner(fs).ScanAsync("/root");

        Assert.Equal("root", result.Root.Name);
        Assert.Equal("/root", result.Root.FullPath);
        Assert.Contains(result.Root.Children, c => c.Name == "docs" && c.FullPath == "/root/docs");
    }

    [Fact]
    public async Task ScanAsync_SkipsLinksToAvoidLoopsAndDoubleCounting()
    {
        var fs = new FakeFileSystemReader()
            .AddFile("/root", "a.txt", 50)
            .AddLink("/root", "shortcut");

        var result = await new DirectoryScanner(fs).ScanAsync("/root");

        Assert.Equal(50, result.Root.SizeInBytes);
        Assert.Single(result.Root.Children);
    }

    [Fact]
    public async Task ScanAsync_FlagsInaccessibleDirectoriesAndKeepsGoing()
    {
        var fs = new FakeFileSystemReader()
            .AddFile("/root", "a.txt", 10)
            .AddSubdirectory("/root", "secret")
            .AddSubdirectory("/root", "open")
            .AddFile("/root/open", "b.txt", 20)
            .MarkInaccessible("/root/secret");

        var result = await new DirectoryScanner(fs).ScanAsync("/root");

        Assert.Equal(30, result.Root.SizeInBytes);
        Assert.Equal(1, result.InaccessibleDirectories);
        Assert.True(result.Root.Children.Single(c => c.Name == "secret").HasAccessErrors);
        Assert.False(result.Root.Children.Single(c => c.Name == "open").HasAccessErrors);
    }

    [Fact]
    public async Task ScanAsync_ThrowsWhenTheRootDoesNotExist()
        => await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => new DirectoryScanner(new FakeFileSystemReader()).ScanAsync("/missing"));

    [Fact]
    public async Task ScanAsync_ThrowsWhenThePathIsBlank()
        => await Assert.ThrowsAnyAsync<ArgumentException>(
            () => new DirectoryScanner(new FakeFileSystemReader()).ScanAsync("  "));

    [Fact]
    public async Task ScanAsync_HonoursCancellation()
    {
        var fs = new FakeFileSystemReader().AddFile("/root", "a.txt", 1);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new DirectoryScanner(fs).ScanAsync("/root", cancellationToken: cts.Token));
    }

    [Fact]
    public async Task ScanAsync_ReportsAFinalProgressWithTheTotals()
    {
        var fs = new FakeFileSystemReader()
            .AddFile("/root", "a.txt", 100)
            .AddFile("/root", "b.txt", 200);
        var progress = new SynchronousProgress<ScanProgress>();

        await new DirectoryScanner(fs).ScanAsync("/root", progress);

        var last = Assert.Single(progress.Reports);
        Assert.Equal(2, last.FilesScanned);
        Assert.Equal(300, last.BytesScanned);
    }

    [Fact]
    public async Task ScanAsync_ReportsProgressPeriodicallyOnLargeFolders()
    {
        var fs = new FakeFileSystemReader();
        for (var i = 0; i < 1200; i++)
        {
            fs.AddFile("/root", $"f{i}.bin", 1);
        }
        var progress = new SynchronousProgress<ScanProgress>();

        await new DirectoryScanner(fs).ScanAsync("/root", progress);

        // Dos avisos intermedios (cada 500 archivos) más el final.
        Assert.Equal(3, progress.Reports.Count);
        Assert.Equal(1200, progress.Reports[^1].FilesScanned);
    }
}
