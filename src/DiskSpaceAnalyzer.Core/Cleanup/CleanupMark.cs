namespace DiskSpaceAnalyzer.Core.Cleanup;

/// <summary>Resultado de clasificar una ruta: qué regla la cubre y de qué forma.</summary>
/// <param name="Rule">Regla que la cubre.</param>
/// <param name="IsRoot">Verdadero si la ruta ES la ubicación de la regla; falso si está dentro de ella.</param>
public sealed record CleanupMark(CleanupRule Rule, bool IsRoot);
