using DiskSpaceAnalyzer.Core.Models;

namespace DiskSpaceAnalyzer.Desktop.ViewModels;

/// <summary>Crea el ViewModel de un nodo; evita que los ViewModels instancien a sus hijos con <c>new</c>.</summary>
public interface INodeViewModelFactory
{
    /// <param name="node">Nodo de dominio a presentar.</param>
    /// <param name="parentSizeInBytes">Tamaño del padre, base para calcular el porcentaje.</param>
    NodeViewModel Create(FileSystemNode node, long parentSizeInBytes);
}
