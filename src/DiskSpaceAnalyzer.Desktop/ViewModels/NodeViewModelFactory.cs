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
    private readonly IUiYield _uiYield;

    public NodeViewModelFactory(
        ISizeFormatter sizeFormatter,
        IFileExplorerService fileExplorer,
        ICleanupClassifier cleanupClassifier,
        IUiYield uiYield)
    {
        ArgumentNullException.ThrowIfNull(sizeFormatter);
        ArgumentNullException.ThrowIfNull(fileExplorer);
        ArgumentNullException.ThrowIfNull(cleanupClassifier);
        ArgumentNullException.ThrowIfNull(uiYield);

        _sizeFormatter = sizeFormatter;
        _fileExplorer = fileExplorer;
        _cleanupClassifier = cleanupClassifier;
        _uiYield = uiYield;
    }

    public NodeViewModel Create(FileSystemNode node, long parentSizeInBytes)
        => new(
            node,
            parentSizeInBytes,
            _sizeFormatter,
            this,
            _fileExplorer,
            _uiYield,
            _cleanupClassifier.Classify(node.FullPath));
}
