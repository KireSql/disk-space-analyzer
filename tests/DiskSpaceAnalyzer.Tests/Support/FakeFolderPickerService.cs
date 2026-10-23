using DiskSpaceAnalyzer.Desktop.Services;

namespace DiskSpaceAnalyzer.Tests.Support;

public sealed class FakeFolderPickerService(string? pickedPath) : IFolderPickerService
{
    public Task<string?> PickFolderAsync() => Task.FromResult(pickedPath);
}
