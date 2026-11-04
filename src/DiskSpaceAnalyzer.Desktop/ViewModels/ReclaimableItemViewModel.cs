using CommunityToolkit.Mvvm.Input;
using DiskSpaceAnalyzer.Core.Cleanup;
using DiskSpaceAnalyzer.Core.Formatting;
using DiskSpaceAnalyzer.Desktop.Services;

namespace DiskSpaceAnalyzer.Desktop.ViewModels;

/// <summary>
/// Una ubicación segura de limpiar del aviso RECUPERABLE: su nombre y peso, y las dos formas de ir a ella
/// (localizarla en el árbol de la aplicación o abrirla en el explorador de archivos). Solo navega: no borra.
/// </summary>
public sealed partial class ReclaimableItemViewModel : ViewModelBase
{
    private readonly CleanupFinding _finding;
    private readonly IFileExplorerService _fileExplorer;
    private readonly Func<string, Task> _revealInTree;

    public ReclaimableItemViewModel(
        CleanupFinding finding,
        ISizeFormatter sizeFormatter,
        IFileExplorerService fileExplorer,
        Func<string, Task> revealInTree)
    {
        ArgumentNullException.ThrowIfNull(finding);
        ArgumentNullException.ThrowIfNull(sizeFormatter);
        ArgumentNullException.ThrowIfNull(fileExplorer);
        ArgumentNullException.ThrowIfNull(revealInTree);

        _finding = finding;
        _fileExplorer = fileExplorer;
        _revealInTree = revealInTree;

        SizeText = sizeFormatter.Format(finding.Node.SizeInBytes);
    }

    public string Name => _finding.Rule.Name;

    /// <summary>Qué contiene y qué pasa si se borra; se muestra como tooltip, no ocupa sitio en el aviso.</summary>
    public string Description => _finding.Rule.Description;

    public string FullPath => _finding.Node.FullPath;

    public string SizeText { get; }

    /// <summary>Localiza la carpeta en el árbol: expande sus carpetas contenedoras y la selecciona.</summary>
    [RelayCommand]
    private Task OpenInApplicationAsync() => _revealInTree(FullPath);

    [RelayCommand]
    private Task OpenInExplorerAsync() => _fileExplorer.OpenAsync(FullPath);
}
