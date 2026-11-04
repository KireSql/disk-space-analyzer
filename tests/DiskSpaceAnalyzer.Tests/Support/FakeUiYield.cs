using DiskSpaceAnalyzer.Desktop.Services;

namespace DiskSpaceAnalyzer.Tests.Support;

/// <summary>
/// Cede el turno sin interfaz real. Por defecto no espera nada; en modo manual cada cesión queda pendiente
/// hasta que el test llama a <see cref="Release"/>, así se puede observar la carga tanda a tanda.
/// </summary>
public sealed class FakeUiYield(bool manual = false) : IUiYield
{
    private TaskCompletionSource? _pending;

    /// <summary>Cuántas veces se cedió el turno (una por tanda terminada, salvo la última).</summary>
    public int Yields { get; private set; }

    public Task YieldAsync()
    {
        Yields++;

        if (!manual)
        {
            return Task.CompletedTask;
        }

        _pending = new TaskCompletionSource();
        return _pending.Task;
    }

    /// <summary>Deja continuar a la tanda en espera; la siguiente tanda se ejecuta antes de volver.</summary>
    public void Release()
    {
        var pending = _pending ?? throw new InvalidOperationException("No hay ninguna tanda en espera.");
        _pending = null;
        pending.SetResult();
    }
}
