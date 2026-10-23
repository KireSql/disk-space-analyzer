using DiskSpaceAnalyzer.Core.Cleanup;
using DiskSpaceAnalyzer.Core.Formatting;
using DiskSpaceAnalyzer.Core.Models;
using DiskSpaceAnalyzer.Desktop.Services;

namespace DiskSpaceAnalyzer.Desktop.ViewModels;

public sealed class NodeViewModelFactory : INodeViewModelFactory
{
    private readonly ISizeFormatter _sizeFormatter;
    private readonly IFileExplorerService _fileExplorer;
    private readonly ICleanupClassifier _cleanupClassifier;

    public NodeViewModelFactory(
        ISizeFormatter sizeFormatter,
        IFileExplorerService fileExplorer,
        ICleanupClassifier cleanupClassifier)
    {
        ArgumentNullException.ThrowIfNull(sizeFormatter);
        ArgumentNullException.ThrowIfNull(fileExplorer);
        ArgumentNullException.ThrowIfNull(cleanupClassifier);

        _sizeFormatter = sizeFormatter;
        _fileExplorer = fileExplorer;
        _cleanupClassifier = cleanupClassifier;
    }

    public NodeViewModel Create(FileSystemNode node, long parentSizeInBytes)
        => new(
            node,
            parentSizeInBytes,
            _sizeFormatter,
            this,
            _fileExplorer,
            _cleanupClassifier.Classify(node.FullPath));
}
