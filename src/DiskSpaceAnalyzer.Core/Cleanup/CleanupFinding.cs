using DiskSpaceAnalyzer.Core.Models;

namespace DiskSpaceAnalyzer.Core.Cleanup;

/// <summary>Una ubicación segura de limpiar que existe dentro de un análisis, con su peso.</summary>
public sealed record CleanupFinding(CleanupRule Rule, FileSystemNode Node);
