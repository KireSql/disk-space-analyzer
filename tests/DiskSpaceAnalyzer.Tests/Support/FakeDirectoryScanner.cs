using DiskSpaceAnalyzer.Core.Abstractions;
using DiskSpaceAnalyzer.Core.Models;
using DiskSpaceAnalyzer.Core.Scanning;

namespace DiskSpaceAnalyzer.Tests.Support;

/// <summary>Escáner de prueba cuyo comportamiento define cada test.</summary>
public sealed class FakeDirectoryScanner(
    Func<string, CancellationToken, Task<ScanResult>> handler) : IDirectoryScanner
{
    public Task<ScanResult> ScanAsync(
        string rootPath,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
        => handler(rootPath, cancellationToken);

    public static FakeDirectoryScanner Returning(ScanResult result)
        => new((_, _) => Task.FromResult(result));

    public static FakeDirectoryScanner Throwing(Exception exception)
        => new((_, _) => Task.FromException<ScanResult>(exception));
}
