namespace DiskSpaceAnalyzer.Core.Cleanup;

/// <summary>Ubicación cuyo contenido se considera seguro de limpiar (caché, temporales, volcados…).</summary>
/// <param name="Id">Identificador estable, único dentro de su catálogo.</param>
/// <param name="Name">Nombre corto para mostrar.</param>
/// <param name="Description">Qué contiene y qué ocurre si se borra.</param>
/// <param name="Path">Ruta absoluta, con las variables de entorno ya resueltas y sin separador final.</param>
public sealed record CleanupRule(string Id, string Name, string Description, string Path);
