using Avalonia;
using Avalonia.Controls;

namespace DiskSpaceAnalyzer.Desktop.Views;

public partial class MainWindow : Window
{
    /// <summary>Por debajo de este ancho (px) se reduce el espaciado y la columna de porcentaje.</summary>
    private const double CompactMaxWidth = 900;

    /// <summary>Por debajo de este ancho (px) se oculta el porcentaje y las tarjetas pasan a dos columnas.</summary>
    private const double NarrowMaxWidth = 600;

    public MainWindow() => InitializeComponent();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ClientSizeProperty)
        {
            UpdateLayoutClasses(ClientSize.Width);
        }
    }

    /// <summary>
    /// Avalonia no tiene media queries: la vista marca la ventana con clases de estilo
    /// (<c>compact</c>, <c>narrow</c>) y el XAML decide qué cambia en cada una.
    /// </summary>
    private void UpdateLayoutClasses(double width)
    {
        Classes.Set("compact", width < CompactMaxWidth);
        Classes.Set("narrow", width < NarrowMaxWidth);
    }
}
