using Avalonia;

namespace DiskSpaceAnalyzer.Desktop;

internal static class Program
{
    // No usar APIs de Avalonia ni código que dependa de SynchronizationContext antes de que
    // arranque la aplicación: todavía no está inicializado.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Configuración de Avalonia; también la usa el previsualizador del IDE.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
