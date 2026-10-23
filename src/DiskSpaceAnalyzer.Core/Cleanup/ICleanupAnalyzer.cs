using DiskSpaceAnalyzer.Core.Models;

namespace DiskSpaceAnalyzer.Core.Cleanup;

/// <summary>Busca en un análisis completo las ubicaciones seguras de limpiar y cuánto espacio ocupan.</summary>
public interface ICleanupAnalyzer
{
    CleanupReport Analyze(DirectoryNode root);
}
