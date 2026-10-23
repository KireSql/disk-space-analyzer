using DiskSpaceAnalyzer.Core.Formatting;
using DiskSpaceAnalyzer.Core.Models;
using DiskSpaceAnalyzer.Desktop.Services;

namespace DiskSpaceAnalyzer.Desktop.ViewModels;

public sealed class NodeViewModelFactory : INodeViewModelFactory
{
    private readonly ISizeFormatter _sizeFormatter;
    private readonly IFileExplorerService _fileExplorer;

    public NodeViewModelFactory(ISizeFormatter sizeFormatter, IFileExplorerService fileExplorer)
    {
        ArgumentNullException.ThrowIfNull(sizeFormatter);
        ArgumentNullException.ThrowIfNull(fileExplorer);

        _sizeFormatter = sizeFormatter;
        _fileExplorer = fileExplorer;
    }

    public NodeViewModel Create(FileSystemNode node, long parentSizeInBytes)
        => new(node, parentSizeInBytes, _sizeFormatter, this, _fileExplorer);
}
