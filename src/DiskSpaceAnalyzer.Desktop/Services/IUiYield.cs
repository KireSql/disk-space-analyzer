namespace DiskSpaceAnalyzer.Desktop.Services;

/// <summary>
/// Cede el turno a la interfaz: la ventana se repinta y atiende al usuario antes de que continúe un trabajo
/// largo hecho por tandas en el hilo de la interfaz (como cargar las filas de una carpeta enorme).
/// </summary>
public interface IUiYield
{
    /// <summary>Se completa cuando la interfaz ha tenido ocasión de trabajar.</summary>
    Task YieldAsync();
}
