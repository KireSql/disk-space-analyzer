using Avalonia.Threading;

namespace DiskSpaceAnalyzer.Desktop.Services;

/// <summary>
/// Se reanuda con prioridad <see cref="DispatcherPriority.Background"/>, la más baja: antes de volver aquí,
/// Avalonia ha hecho el diseño, el pintado y ha atendido el ratón y el teclado. Con una pausa normal
/// (<c>Task.Delay</c>) la continuación se cuela por delante y la ventana no responde hasta que termina todo.
/// </summary>
public sealed class AvaloniaUiYield : IUiYield
{
    public Task YieldAsync()
        => Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Background).GetTask();
}
