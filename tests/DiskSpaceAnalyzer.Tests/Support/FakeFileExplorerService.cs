using DiskSpaceAnalyzer.Desktop.Services;

namespace DiskSpaceAnalyzer.Tests.Support;

/// <summary>Registra las rutas que se pidió abrir, en lugar de abrir un explorador real.</summary>
public sealed class FakeFileExplorerService : IFileExplorerService
{
    public List<string> OpenedPaths { get; } = [];

    public Task OpenAsync(string path)
    {
        OpenedPaths.Add(path);
        return Task.CompletedTask;
    }
}
