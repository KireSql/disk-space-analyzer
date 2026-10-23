namespace DiskSpaceAnalyzer.Core.Models;

/// <summary>
/// Nodo compuesto del árbol: una carpeta y su contenido. Es inmutable; el tamaño se calcula
/// una sola vez al construirla porque el árbol se crea de abajo hacia arriba.
/// </summary>
public sealed class DirectoryNode : FileSystemNode
{
    public DirectoryNode(
        string name,
        string fullPath,
        IEnumerable<FileSystemNode> children,
        bool hasAccessErrors = false)
        : base(name, fullPath)
    {
        ArgumentNullException.ThrowIfNull(children);

        Children = children.ToArray();
        HasAccessErrors = hasAccessErrors;
        SizeInBytes = Children.Sum(child => child.SizeInBytes);
    }

    public override NodeKind Kind => NodeKind.Directory;

    public override long SizeInBytes { get; }

    public override IReadOnlyList<FileSystemNode> Children { get; }

    public override bool HasAccessErrors { get; }
}
