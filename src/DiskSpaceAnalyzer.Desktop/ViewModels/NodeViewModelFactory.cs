using DiskSpaceAnalyzer.Core.Formatting;
using DiskSpaceAnalyzer.Core.Models;

namespace DiskSpaceAnalyzer.Desktop.ViewModels;

public sealed class NodeViewModelFactory : INodeViewModelFactory
{
    private readonly ISizeFormatter _sizeFormatter;

    public NodeViewModelFactory(ISizeFormatter sizeFormatter)
    {
        ArgumentNullException.ThrowIfNull(sizeFormatter);
        _sizeFormatter = sizeFormatter;
    }

    public NodeViewModel Create(FileSystemNode node, long parentSizeInBytes)
        => new(node, parentSizeInBytes, _sizeFormatter, this);
}
