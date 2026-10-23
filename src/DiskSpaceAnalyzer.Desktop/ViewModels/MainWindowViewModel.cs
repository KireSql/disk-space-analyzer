using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskSpaceAnalyzer.Core.Abstractions;
using DiskSpaceAnalyzer.Core.Formatting;
using DiskSpaceAnalyzer.Core.Models;
using DiskSpaceAnalyzer.Core.Scanning;
using DiskSpaceAnalyzer.Desktop.Services;

namespace DiskSpaceAnalyzer.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IDirectoryScanner _scanner;
    private readonly IFolderPickerService _folderPicker;
    private readonly INodeViewModelFactory _nodeFactory;
    private readonly ISizeFormatter _sizeFormatter;

    public MainWindowViewModel(
        IDirectoryScanner scanner,
        IFolderPickerService folderPicker,
        INodeViewModelFactory nodeFactory,
        ISizeFormatter sizeFormatter)
    {
        ArgumentNullException.ThrowIfNull(scanner);
        ArgumentNullException.ThrowIfNull(folderPicker);
        ArgumentNullException.ThrowIfNull(nodeFactory);
        ArgumentNullException.ThrowIfNull(sizeFormatter);

        _scanner = scanner;
        _folderPicker = folderPicker;
        _nodeFactory = nodeFactory;
        _sizeFormatter = sizeFormatter;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    public partial string SelectedPath { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    public partial bool IsScanning { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Elige una carpeta y pulsa «Analizar».";

    /// <summary>Cifras del último análisis completado; <c>null</c> mientras no haya resultado.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSummary), nameof(ShowEmptyState))]
    public partial ScanSummaryViewModel? Summary { get; set; }

    public bool HasSummary => Summary is not null;

    /// <summary>Verdadero cuando no hay nada que mostrar ni se está analizando: invita a elegir una carpeta.</summary>
    public bool ShowEmptyState => !IsScanning && Summary is null;

    /// <summary>Raíz del árbol mostrado (una sola, pero el TreeView espera una colección).</summary>
    public ObservableCollection<NodeViewModel> RootNodes { get; } = [];

    [RelayCommand]
    private async Task BrowseAsync()
    {
        var path = await _folderPicker.PickFolderAsync();
        if (path is not null)
        {
            SelectedPath = path;
        }
    }

    private bool CanScan() => !string.IsNullOrWhiteSpace(SelectedPath);

    // IncludeCancelCommand genera ScanCancelCommand, que cancela el token recibido aquí.
    [RelayCommand(CanExecute = nameof(CanScan), IncludeCancelCommand = true)]
    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        IsScanning = true;
        Summary = null;
        RootNodes.Clear();
        StatusMessage = "Analizando…";

        try
        {
            // Se crea en el hilo de UI, por lo que los avances se publican de vuelta en él.
            var progress = new Progress<ScanProgress>(ShowProgress);
            var result = await _scanner.ScanAsync(SelectedPath, progress, cancellationToken);

            var root = _nodeFactory.Create(result.Root, result.Root.SizeInBytes);
            root.IsExpanded = true;
            RootNodes.Add(root);

            Summary = new ScanSummaryViewModel(result, _sizeFormatter);
            StatusMessage = BuildCompletionMessage(result);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Análisis cancelado.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            StatusMessage = $"No se pudo analizar la carpeta: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    private void ShowProgress(ScanProgress progress)
        => StatusMessage = $"Analizando… {progress.FilesScanned:N0} archivos · "
                           + $"{_sizeFormatter.Format(progress.BytesScanned)} · {progress.CurrentPath}";

    // Las cifras detalladas van en las tarjetas de resumen; aquí solo el cierre y las advertencias.
    private static string BuildCompletionMessage(ScanResult result)
    {
        var message = $"Análisis completado en {result.Elapsed.TotalSeconds:0.0} s.";

        return result.InaccessibleDirectories switch
        {
            0 => message,
            1 => $"{message} 1 carpeta sin acceso (marcada con ⚠).",
            var count => $"{message} {count:N0} carpetas sin acceso (marcadas con ⚠).",
        };
    }
}
