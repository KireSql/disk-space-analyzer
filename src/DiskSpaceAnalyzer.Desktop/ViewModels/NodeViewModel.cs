using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskSpaceAnalyzer.Core.Cleanup;
using DiskSpaceAnalyzer.Core.Formatting;
using DiskSpaceAnalyzer.Core.Models;
using DiskSpaceAnalyzer.Desktop.Services;

namespace DiskSpaceAnalyzer.Desktop.ViewModels;

/// <summary>
/// Presenta un nodo del dominio en el árbol. Los hijos se crean la primera vez que se piden,
/// así solo se materializan los niveles que el usuario llega a expandir.
/// </summary>
public sealed partial class NodeViewModel : ViewModelBase
{
    private readonly FileSystemNode _node;
    private readonly INodeViewModelFactory _factory;
    private readonly IFileExplorerService _fileExplorer;
    private readonly CleanupMark? _cleanupMark;
    private IReadOnlyList<NodeViewModel>? _children;

    /// <param name="cleanupMark">Si la ruta es (o está dentro de) una ubicación segura de limpiar; <c>null</c> si no.</param>
    public NodeViewModel(
        FileSystemNode node,
        long parentSizeInBytes,
        ISizeFormatter sizeFormatter,
        INodeViewModelFactory factory,
        IFileExplorerService fileExplorer,
        CleanupMark? cleanupMark = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(sizeFormatter);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(fileExplorer);

        _node = node;
        _factory = factory;
        _fileExplorer = fileExplorer;
        _cleanupMark = cleanupMark;

        SizeText = sizeFormatter.Format(node.SizeInBytes);
        Percent = parentSizeInBytes > 0 ? node.SizeInBytes * 100d / parentSizeInBytes : 0;
    }

    public string Name => _node.Name;

    public string FullPath => _node.FullPath;

    public bool IsDirectory => _node.Kind == NodeKind.Directory;

    public bool HasAccessErrors => _node.HasAccessErrors;

    /// <summary>La ruta es, o está dentro de, una ubicación segura de limpiar.</summary>
    public bool IsCleanupMarked => _cleanupMark is not null;

    /// <summary>La ruta ES la ubicación segura (la que recibe la etiqueta y el color de fila).</summary>
    public bool IsCleanupRoot => _cleanupMark is { IsRoot: true };

    // La vista elige icono con un solo booleano por elemento, así que estos dos cubren los casos.
    public bool IsPlainDirectory => IsDirectory && !IsCleanupMarked;

    public bool IsCleanupDirectory => IsDirectory && IsCleanupMarked;

    /// <summary>Texto del tooltip de la fila; <c>null</c> (sin tooltip) si no es limpiable.</summary>
    public string? CleanupTooltip => _cleanupMark switch
    {
        null => null,
        { IsRoot: true } mark => $"Seguro de limpiar: {mark.Rule.Name}\n{mark.Rule.Description}",
        var mark => $"Dentro de una ubicación segura de limpiar: «{mark.Rule.Name}»\n{mark.Rule.Description}",
    };

    public string SizeText { get; }

    /// <summary>Porcentaje (0–100) que representa respecto a su carpeta contenedora.</summary>
    public double Percent { get; }

    public string PercentText => Percent.ToString("0.#", CultureInfo.CurrentCulture) + " %";

    /// <summary>Hijos ordenados de mayor a menor tamaño.</summary>
    public IReadOnlyList<NodeViewModel> Children => _children ??= CreateChildren();

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    /// <summary>Abre la carpeta (o la carpeta contenedora, si es un archivo) en el explorador del sistema.</summary>
    [RelayCommand]
    private Task OpenInExplorerAsync() => _fileExplorer.OpenAsync(FullPath);

    private NodeViewModel[] CreateChildren()
        => _node.Children
            .OrderByDescending(child => child.SizeInBytes)
            .ThenBy(child => child.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(child => _factory.Create(child, _node.SizeInBytes))
            .ToArray();
}
