using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskSpaceAnalyzer.Core.Cleanup;
using DiskSpaceAnalyzer.Core.Formatting;
using DiskSpaceAnalyzer.Core.Models;
using DiskSpaceAnalyzer.Desktop.Services;

namespace DiskSpaceAnalyzer.Desktop.ViewModels;

/// <summary>
/// Presenta un nodo del dominio en el árbol. Los hijos se crean al expandir la carpeta y se publican por
/// tandas: crear de golpe miles de filas congela la ventana (el TreeView no virtualiza), así que mientras
/// llegan el árbol muestra una fila "Cargando…" con un indicador y la interfaz sigue respondiendo.
/// </summary>
public sealed partial class NodeViewModel : ViewModelBase
{
    /// <summary>Filas que se añaden por tanda. Cada tanda ocupa a la interfaz solo unas decenas de milisegundos.</summary>
    private const int RowsPerBatch = 25;

    /// <summary>Por encima de este número de hijos, el ordenado se hace fuera del hilo de la interfaz.</summary>
    private const int SortOffThreadThreshold = 5_000;

    private const string LoadingName = "Cargando…";

    private readonly FileSystemNode _node;
    private readonly ISizeFormatter _sizeFormatter;
    private readonly INodeViewModelFactory _factory;
    private readonly IFileExplorerService _fileExplorer;
    private readonly IUiYield _uiYield;
    private readonly CleanupMark? _cleanupMark;
    private Task? _loading;

    /// <param name="uiYield">Cede el turno a la interfaz entre tanda y tanda al cargar los hijos.</param>
    /// <param name="cleanupMark">Si la ruta es (o está dentro de) una ubicación segura de limpiar; <c>null</c> si no.</param>
    public NodeViewModel(
        FileSystemNode node,
        long parentSizeInBytes,
        ISizeFormatter sizeFormatter,
        INodeViewModelFactory factory,
        IFileExplorerService fileExplorer,
        IUiYield uiYield,
        CleanupMark? cleanupMark = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(sizeFormatter);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(fileExplorer);
        ArgumentNullException.ThrowIfNull(uiYield);

        _node = node;
        _sizeFormatter = sizeFormatter;
        _factory = factory;
        _fileExplorer = fileExplorer;
        _uiYield = uiYield;
        _cleanupMark = cleanupMark;

        SizeText = sizeFormatter.Format(node.SizeInBytes);
        Percent = parentSizeInBytes > 0 ? node.SizeInBytes * 100d / parentSizeInBytes : 0;

        // Una carpeta con contenido arranca con la fila "Cargando…": así el árbol muestra la flecha de
        // expandir sin haber creado todavía ninguna fila real.
        if (IsDirectory && node.Children.Count > 0)
        {
            Children.Add(CreateLoadingRow());
        }
    }

    public string Name => _node.Name;

    public string FullPath => _node.FullPath;

    public bool IsDirectory => _node.Kind == NodeKind.Directory;

    /// <summary>Fila provisional "Cargando…" que se muestra mientras llegan los hijos de una carpeta.</summary>
    public bool IsPlaceholder { get; private init; }

    public bool IsFile => !IsPlaceholder && !IsDirectory;

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

    /// <summary>
    /// Hijos ordenados de mayor a menor tamaño. Hasta que termina la carga, la última fila es la
    /// provisional "Cargando…" (<see cref="IsPlaceholder"/>).
    /// </summary>
    public ObservableCollection<NodeViewModel> Children { get; } = [];

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    /// <summary>Verdadero mientras la carpeta sigue recibiendo filas; la propia carpeta muestra el indicador.</summary>
    [ObservableProperty]
    public partial bool IsLoadingChildren { get; set; }

    /// <summary>Progreso de la carga ("100 de 2,000") que la propia carpeta muestra junto a su indicador.</summary>
    [ObservableProperty]
    public partial string LoadingProgress { get; set; } = string.Empty;

    /// <summary>Texto de la fila provisional: cuántos elementos van cargados.</summary>
    [ObservableProperty]
    public partial string LoadingText { get; set; } = LoadingName;

    /// <summary>
    /// Termina de cargar los hijos (si no lo estaban ya) y devuelve cuando están todos. Se llama sola al
    /// expandir la carpeta; es seguro llamarla varias veces.
    /// </summary>
    public Task LoadChildrenAsync() => _loading ??= LoadChildrenCoreAsync();

    /// <summary>Abre la carpeta (o la carpeta contenedora, si es un archivo) en el explorador del sistema.</summary>
    [RelayCommand]
    private Task OpenInExplorerAsync() => _fileExplorer.OpenAsync(FullPath);

    /// <summary>
    /// Expande las carpetas necesarias hasta llegar a la ruta indicada y devuelve su fila, o <c>null</c> si
    /// la ruta no está en este árbol.
    /// </summary>
    public async Task<NodeViewModel?> RevealAsync(string path, StringComparison comparison)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (string.Equals(FullPath, path, comparison))
        {
            return this;
        }

        if (!IsDirectory || !IsAncestorOf(path, comparison))
        {
            return null;
        }

        await LoadChildrenAsync();

        foreach (var child in Children.Where(child => !child.IsPlaceholder).ToArray())
        {
            var found = await child.RevealAsync(path, comparison);
            if (found is not null)
            {
                // Solo se expande si la ruta está de verdad por debajo: una ruta desconocida no deja el árbol abierto.
                IsExpanded = true;
                return found;
            }
        }

        return null;
    }

    private async Task LoadChildrenCoreAsync()
    {
        if (Children.Count == 0 || !Children[0].IsPlaceholder)
        {
            return;
        }

        var loadingRow = Children[0];
        var parentSize = _node.SizeInBytes;
        var source = _node.Children;

        var sorted = source.Count > SortOffThreadThreshold
            ? await Task.Run(() => SortBySize(source))
            : SortBySize(source);

        for (var i = 0; i < sorted.Length; i++)
        {
            // La fila "Cargando…" se queda siempre la última.
            Children.Insert(Children.Count - 1, _factory.Create(sorted[i], parentSize));

            var loaded = i + 1;
            if (loaded % RowsPerBatch == 0 && loaded < sorted.Length)
            {
                IsLoadingChildren = true;
                LoadingProgress = $"{loaded:N0} de {sorted.Length:N0}";
                loadingRow.LoadingText = $"Cargando… {LoadingProgress}";
                await _uiYield.YieldAsync();
            }
        }

        Children.Remove(loadingRow);
        IsLoadingChildren = false;
        LoadingProgress = string.Empty;
    }

    private static FileSystemNode[] SortBySize(IReadOnlyList<FileSystemNode> nodes)
        => nodes
            .OrderByDescending(child => child.SizeInBytes)
            .ThenBy(child => child.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

    private NodeViewModel CreateLoadingRow()
        => new(new FileNode(LoadingName, LoadingName, 0), 0, _sizeFormatter, _factory, _fileExplorer, _uiYield)
        {
            IsPlaceholder = true,
        };

    /// <summary>Verdadero si <paramref name="path"/> está estrictamente dentro de esta carpeta.</summary>
    private bool IsAncestorOf(string path, StringComparison comparison)
    {
        if (path.Length <= FullPath.Length || !path.StartsWith(FullPath, comparison))
        {
            return false;
        }

        // Evita tomar "C:\Datos2" por una subcarpeta de "C:\Datos": tras el prefijo debe venir un separador
        // (o el propio prefijo ya termina en uno, como "C:\").
        return IsSeparator(FullPath[^1]) || IsSeparator(path[FullPath.Length]);
    }

    private static bool IsSeparator(char c) => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;

    partial void OnIsExpandedChanged(bool value)
    {
        if (value)
        {
            _ = LoadChildrenAsync();
        }
    }
}
