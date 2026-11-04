using System.Globalization;
using DiskSpaceAnalyzer.Core.Abstractions;
using DiskSpaceAnalyzer.Core.Cleanup;
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

    private readonly FakeFileExplorerService _explorer = new();

    private MainWindowViewModel CreateViewModel(
        IDirectoryScanner scanner,
        string? pickedPath = null,
        CleanupAdvisor? cleanup = null)
    {
        cleanup ??= NoCleanup.Advisor;

        return new MainWindowViewModel(
            scanner,
            new FakeFolderPickerService(pickedPath),
            new NodeViewModelFactory(Formatter, _explorer, cleanup, new FakeUiYield()),
            Formatter,
            cleanup,
            _explorer);
    }

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
    public async Task Scan_ReportsReclaimableSpaceWhenTheTreeContainsASafeLocation()
    {
        var rule = new CleanupRule("root", "Raíz de prueba", "Descripción.", "/root");
        var cleanup = new CleanupAdvisor([rule], StringComparison.Ordinal);
        var vm = CreateViewModel(FakeDirectoryScanner.Returning(SampleResult()), cleanup: cleanup);
        vm.SelectedPath = "/root";

        await vm.ScanCommand.ExecuteAsync(null);

        Assert.True(vm.Summary?.HasReclaimable);
        Assert.Equal("2 KB", vm.Summary?.ReclaimableSize);
        Assert.True(vm.RootNodes[0].IsCleanupRoot);
    }

    [Fact]
    public async Task Scan_ReportsNothingReclaimableWithoutRules()
    {
        var vm = CreateViewModel(FakeDirectoryScanner.Returning(SampleResult()));
        vm.SelectedPath = "/root";

        await vm.ScanCommand.ExecuteAsync(null);

        Assert.False(vm.Summary?.HasReclaimable);
        Assert.False(vm.RootNodes[0].IsCleanupMarked);
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

    // ---- Aviso RECUPERABLE: abrir en la aplicación / en el explorador ----

    // Rutas reales del sistema (no "/root/..."): el motor de reglas compara con el separador de cada SO.
    private static readonly string NestedRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "dsa-reveal"));
    private static readonly string NestedCache = Path.Combine(NestedRoot, "cache");
    private static readonly string NestedTemp = Path.Combine(NestedCache, "temp");

    private static ScanResult NestedResult() => new(
        new DirectoryNode(
            "dsa-reveal",
            NestedRoot,
            [
                new DirectoryNode(
                    "cache",
                    NestedCache,
                    [new DirectoryNode("temp", NestedTemp, [new FileNode("a.tmp", Path.Combine(NestedTemp, "a.tmp"), 4096)])]),
                new FileNode("keep.txt", Path.Combine(NestedRoot, "keep.txt"), 10),
            ]),
        FilesScanned: 2,
        DirectoriesScanned: 3,
        InaccessibleDirectories: 0,
        Elapsed: TimeSpan.FromSeconds(1));

    private static CleanupAdvisor TempRule()
        => new([new CleanupRule("temp", "Temporales", "Se regeneran.", NestedTemp)], CleanupAdvisor.DefaultComparison);

    [Fact]
    public async Task OpeningARecoverableLocationInTheApplication_ExpandsTheTreeAndSelectsIt()
    {
        var vm = CreateViewModel(FakeDirectoryScanner.Returning(NestedResult()), cleanup: TempRule());
        vm.SelectedPath = NestedRoot;
        await vm.ScanCommand.ExecuteAsync(null);
        var item = Assert.Single(vm.Summary!.Reclaimable);

        await item.OpenInApplicationCommand.ExecuteAsync(null);

        Assert.Equal(NestedTemp, vm.SelectedNode?.FullPath);
        Assert.True(vm.RootNodes[0].Children.Single(c => c.Name == "cache").IsExpanded);
    }

    [Fact]
    public async Task OpeningARecoverableLocationInTheExplorer_DoesNotTouchTheTree()
    {
        var vm = CreateViewModel(FakeDirectoryScanner.Returning(NestedResult()), cleanup: TempRule());
        vm.SelectedPath = NestedRoot;
        await vm.ScanCommand.ExecuteAsync(null);
        var item = Assert.Single(vm.Summary!.Reclaimable);

        await item.OpenInExplorerCommand.ExecuteAsync(null);

        Assert.Equal([NestedTemp], _explorer.OpenedPaths);
        Assert.Null(vm.SelectedNode);
    }

    [Fact]
    public async Task RevealInTree_WithAnUnknownPath_KeepsTheCurrentSelection()
    {
        var vm = CreateViewModel(FakeDirectoryScanner.Returning(NestedResult()));
        vm.SelectedPath = NestedRoot;
        await vm.ScanCommand.ExecuteAsync(null);

        await vm.RevealInTreeAsync("/elsewhere");

        Assert.Null(vm.SelectedNode);
    }

    [Fact]
    public async Task RevealInTree_WithoutAResult_DoesNothing()
    {
        var vm = CreateViewModel(FakeDirectoryScanner.Returning(NestedResult()));

        await vm.RevealInTreeAsync(NestedCache);

        Assert.Null(vm.SelectedNode);
    }

    [Fact]
    public async Task NewScan_ClearsTheSelection()
    {
        var vm = CreateViewModel(FakeDirectoryScanner.Returning(NestedResult()));
        vm.SelectedPath = NestedRoot;
        await vm.ScanCommand.ExecuteAsync(null);
        await vm.RevealInTreeAsync(NestedCache);
        Assert.NotNull(vm.SelectedNode);

        await vm.ScanCommand.ExecuteAsync(null);

        Assert.Equal(NestedRoot, vm.RootNodes.Single().FullPath);
        Assert.Null(vm.SelectedNode);
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
