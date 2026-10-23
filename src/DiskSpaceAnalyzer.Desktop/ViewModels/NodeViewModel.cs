using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using DiskSpaceAnalyzer.Core.Formatting;
using DiskSpaceAnalyzer.Core.Models;

namespace DiskSpaceAnalyzer.Desktop.ViewModels;

/// <summary>
/// Presenta un nodo del dominio en el árbol. Los hijos se crean la primera vez que se piden,
/// así solo se materializan los niveles que el usuario llega a expandir.
/// </summary>
public sealed partial class NodeViewModel : ViewModelBase
{
    private readonly FileSystemNode _node;
    private readonly INodeViewModelFactory _factory;
    private IReadOnlyList<NodeViewModel>? _children;

    public NodeViewModel(
        FileSystemNode node,
        long parentSizeInBytes,
        ISizeFormatter sizeFormatter,
        INodeViewModelFactory factory)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(sizeFormatter);
        ArgumentNullException.ThrowIfNull(factory);

        _node = node;
        _factory = factory;

        SizeText = sizeFormatter.Format(node.SizeInBytes);
        Percent = parentSizeInBytes > 0 ? node.SizeInBytes * 100d / parentSizeInBytes : 0;
    }

    public string Name => _node.Name;

    public string FullPath => _node.FullPath;

    public bool IsDirectory => _node.Kind == NodeKind.Directory;

    public bool HasAccessErrors => _node.HasAccessErrors;

    public string SizeText { get; }

    /// <summary>Porcentaje (0–100) que representa respecto a su carpeta contenedora.</summary>
    public double Percent { get; }

    public string PercentText => Percent.ToString("0.#", CultureInfo.CurrentCulture) + " %";

    /// <summary>Hijos ordenados de mayor a menor tamaño.</summary>
    public IReadOnlyList<NodeViewModel> Children => _children ??= CreateChildren();

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    private NodeViewModel[] CreateChildren()
        => _node.Children
            .OrderByDescending(child => child.SizeInBytes)
            .ThenBy(child => child.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(child => _factory.Create(child, _node.SizeInBytes))
            .ToArray();
}
