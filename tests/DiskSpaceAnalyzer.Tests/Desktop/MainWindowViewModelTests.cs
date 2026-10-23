using System.Globalization;
using DiskSpaceAnalyzer.Core.Abstractions;
using DiskSpaceAnalyzer.Core.Formatting;
using DiskSpaceAnalyzer.Core.Models;
using DiskSpaceAnalyzer.Desktop.ViewModels;
using DiskSpaceAnalyzer.Tests.Support;

namespace DiskSpaceAnalyzer.Tests.Desktop;

public class MainWindowViewModelTests
{
    private static readonly BinarySizeFormatter Formatter = new(CultureInfo.InvariantCulture);

    /// <summary>Duración de <see cref="SampleResult"/> tal como la formatea la cultura actual (1,5 o 1.5).</summary>
    private static readonly string ElapsedText = $"{1.5:0.0} s";

    private static ScanResult SampleResult(int inaccessible = 0) => new(
        new DirectoryNode("root", "/root", [new FileNode("a.bin", "/root/a.bin", 2048)]),
        FilesScanned: 1,
        DirectoriesScanned: 1,
        InaccessibleDirectories: inaccessible,
        Elapsed: TimeSpan.FromSeconds(1.5));

    private static MainWindowViewModel CreateViewModel(IDirectoryScanner scanner, string? pickedPath = null)
        => new(
            scanner,
            new FakeFolderPickerService(pickedPath),
            new NodeViewModelFactory(Formatter, new FakeFileExplorerService()),
            Formatter);

    [Fact]
    public void ScanCommand_CannotRunWithoutAPath()
    {
        var vm = CreateViewModel(FakeDirectoryScanner.Returning(SampleResult()));

        Assert.False(vm.ScanCommand.CanExecute(null));

        vm.SelectedPath = "/root";

        Assert.True(vm.ScanCommand.CanExecute(null));
    }

    [Fact]
    public void InitialState_ShowsTheEmptyState()
    {
        var vm = CreateViewModel(FakeDirectoryScanner.Returning(SampleResult()));

        Assert.True(vm.ShowEmptyState);
        Assert.False(vm.HasSummary);
        Assert.Empty(vm.RootNodes);
    }

    [Fact]
    public async Task Scan_PublishesTheTreeAndTheSummary()
    {
        var vm = CreateViewModel(FakeDirectoryScanner.Returning(SampleResult()));
        vm.SelectedPath = "/root";

        await vm.ScanCommand.ExecuteAsync(null);

        var root = Assert.Single(vm.RootNodes);
        Assert.True(root.IsExpanded);
        Assert.Equal("2 KB", vm.Summary?.TotalSize);
        Assert.Equal(ElapsedText, vm.Summary?.Elapsed);
        Assert.True(vm.HasSummary);
        Assert.False(vm.ShowEmptyState);
        Assert.False(vm.IsScanning);
    }

    [Theory]
    [InlineData(0, "")]
    [InlineData(1, " 1 carpeta sin acceso (marcada con ⚠).")]
    [InlineData(3, " 3 carpetas sin acceso (marcadas con ⚠).")]
    public async Task Scan_DescribesInaccessibleFoldersWithTheRightPlural(int inaccessible, string expectedSuffix)
    {
        var vm = CreateViewModel(FakeDirectoryScanner.Returning(SampleResult(inaccessible)));
        vm.SelectedPath = "/root";

        await vm.ScanCommand.ExecuteAsync(null);

        Assert.Equal($"Análisis completado en {ElapsedText}.{expectedSuffix}", vm.StatusMessage);
    }

    [Fact]
    public async Task Scan_ReportsErrorsInTheStatusAndLeavesNoResult()
    {
        var vm = CreateViewModel(FakeDirectoryScanner.Throwing(new DirectoryNotFoundException("No existe la carpeta.")));
        vm.SelectedPath = "/missing";

        await vm.ScanCommand.ExecuteAsync(null);

        Assert.Contains("No existe la carpeta.", vm.StatusMessage);
        Assert.Empty(vm.RootNodes);
        Assert.Null(vm.Summary);
        Assert.False(vm.IsScanning);
    }

    [Fact]
    public async Task Scan_CanBeCancelled()
    {
        var started = new TaskCompletionSource();
        var scanner = new FakeDirectoryScanner(async (_, ct) =>
        {
            started.SetResult();
            await Task.Delay(Timeout.Infinite, ct);
            return SampleResult();
        });
        var vm = CreateViewModel(scanner);
        vm.SelectedPath = "/root";

        var running = vm.ScanCommand.ExecuteAsync(null);
        await started.Task;
        Assert.True(vm.IsScanning);

        vm.ScanCancelCommand.Execute(null);
        await running;

        Assert.Equal("Análisis cancelado.", vm.StatusMessage);
        Assert.False(vm.IsScanning);
        Assert.Null(vm.Summary);
    }

    [Fact]
    public async Task NewScan_ClearsThePreviousResultEvenWhenItFails()
    {
        var calls = 0;
        var scanner = new FakeDirectoryScanner((_, _) => ++calls == 1
            ? Task.FromResult(SampleResult())
            : Task.FromException<ScanResult>(new IOException("falló")));
        var vm = CreateViewModel(scanner);
        vm.SelectedPath = "/root";

        await vm.ScanCommand.ExecuteAsync(null);
        Assert.True(vm.HasSummary);

        await vm.ScanCommand.ExecuteAsync(null);

        Assert.Empty(vm.RootNodes);
        Assert.Null(vm.Summary);
        Assert.True(vm.ShowEmptyState);
    }

    [Fact]
    public async Task Browse_SetsThePathWhenTheUserPicksAFolder()
    {
        var vm = CreateViewModel(FakeDirectoryScanner.Returning(SampleResult()), pickedPath: "/chosen");

        await vm.BrowseCommand.ExecuteAsync(null);

        Assert.Equal("/chosen", vm.SelectedPath);
    }

    [Fact]
    public async Task Browse_KeepsThePathWhenTheUserCancels()
    {
        var vm = CreateViewModel(FakeDirectoryScanner.Returning(SampleResult()), pickedPath: null);
        vm.SelectedPath = "/previous";

        await vm.BrowseCommand.ExecuteAsync(null);

        Assert.Equal("/previous", vm.SelectedPath);
    }
}
