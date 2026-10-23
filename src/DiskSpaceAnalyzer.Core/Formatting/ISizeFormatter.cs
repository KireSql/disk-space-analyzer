namespace DiskSpaceAnalyzer.Core.Formatting;

/// <summary>Estrategia para presentar un tamaño en bytes como texto legible.</summary>
public interface ISizeFormatter
{
    string Format(long sizeInBytes);
}
