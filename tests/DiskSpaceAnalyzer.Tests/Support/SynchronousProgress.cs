namespace DiskSpaceAnalyzer.Tests.Support;

/// <summary>
/// <see cref="IProgress{T}"/> que registra los avances de forma síncrona.
/// <see cref="Progress{T}"/> los publica de forma asíncrona y haría las pruebas no deterministas.
/// </summary>
public sealed class SynchronousProgress<T> : IProgress<T>
{
    public List<T> Reports { get; } = [];

    public void Report(T value) => Reports.Add(value);
}
