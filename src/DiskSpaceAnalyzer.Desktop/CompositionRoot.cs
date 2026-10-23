using Avalonia.Controls;
using DiskSpaceAnalyzer.Core.Abstractions;
using DiskSpaceAnalyzer.Core.Cleanup;
using DiskSpaceAnalyzer.Core.Formatting;
using DiskSpaceAnalyzer.Core.Scanning;
using DiskSpaceAnalyzer.Desktop.Services;
using DiskSpaceAnalyzer.Desktop.ViewModels;
using DiskSpaceAnalyzer.Infrastructure.Cleanup;
using DiskSpaceAnalyzer.Infrastructure.FileSystem;
using Microsoft.Extensions.DependencyInjection;

namespace DiskSpaceAnalyzer.Desktop;

/// <summary>
/// Único lugar donde se conocen las implementaciones concretas y se cablean las dependencias.
/// El resto de la aplicación solo depende de abstracciones.
/// </summary>
internal static class CompositionRoot
{
    public static ServiceProvider Build(Func<TopLevel?> topLevelAccessor)
    {
        ArgumentNullException.ThrowIfNull(topLevelAccessor);

        var services = new ServiceCollection();

        // Núcleo e infraestructura
        services.AddSingleton<IFileSystemReader, PhysicalFileSystemReader>();
        services.AddSingleton<IDirectoryScanner, DirectoryScanner>();
        services.AddSingleton<ISizeFormatter>(_ => new BinarySizeFormatter());

        // Ubicaciones seguras de limpiar (solo lectura: se marcan, no se borran). Un único objeto hace de
        // clasificador (por ruta) y de analizador (sobre el árbol).
        services.AddSingleton<ICleanupCatalog>(_ => new EmbeddedCleanupCatalog());
        services.AddSingleton(sp => new CleanupAdvisor(
            sp.GetRequiredService<ICleanupCatalog>().GetRules(),
            CleanupAdvisor.DefaultComparison));
        services.AddSingleton<ICleanupClassifier>(sp => sp.GetRequiredService<CleanupAdvisor>());
        services.AddSingleton<ICleanupAnalyzer>(sp => sp.GetRequiredService<CleanupAdvisor>());

        // Presentación
        services.AddSingleton<IFolderPickerService>(_ => new AvaloniaFolderPickerService(topLevelAccessor));
        services.AddSingleton<IFileExplorerService>(_ => new AvaloniaFileExplorerService(topLevelAccessor));
        services.AddSingleton<INodeViewModelFactory, NodeViewModelFactory>();
        services.AddTransient<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }
}
