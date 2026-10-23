namespace DiskSpaceAnalyzer.Core.Cleanup;

/// <summary>Indica si una ruta es, o está dentro de, una ubicación segura de limpiar.</summary>
public interface ICleanupClassifier
{
    /// <returns>La marca, o <c>null</c> si la ruta no está cubierta por ninguna regla.</returns>
    CleanupMark? Classify(string fullPath);
}
