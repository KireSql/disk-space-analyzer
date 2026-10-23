namespace DiskSpaceAnalyzer.Core.Cleanup;

/// <summary>Origen de las reglas de limpieza del sistema operativo actual.</summary>
public interface ICleanupCatalog
{
    /// <summary>Reglas aplicables a esta máquina. Vacío si el sistema operativo no tiene catálogo.</summary>
    IReadOnlyList<CleanupRule> GetRules();
}
