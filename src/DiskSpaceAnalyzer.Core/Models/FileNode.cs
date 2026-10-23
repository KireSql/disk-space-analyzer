namespace DiskSpaceAnalyzer.Core.Models;

/// <summary>Hoja del árbol: un archivo con su tamaño.</summary>
public sealed class FileNode : FileSystemNode
{
    public FileNode(string name, string fullPath, long sizeInBytes)
        : base(name, fullPath)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sizeInBytes);
        SizeInBytes = sizeInBytes;
    }

    public override NodeKind Kind => NodeKind.File;

    public override long SizeInBytes { get; }

    public override IReadOnlyList<FileSystemNode> Children => [];
}
