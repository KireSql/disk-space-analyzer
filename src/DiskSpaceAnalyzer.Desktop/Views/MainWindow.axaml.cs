using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using DiskSpaceAnalyzer.Desktop.ViewModels;

namespace DiskSpaceAnalyzer.Desktop.Views;

public partial class MainWindow : Window
{
    /// <summary>Por debajo de este ancho (px) se reduce el espaciado y la columna de porcentaje.</summary>
    private const double CompactMaxWidth = 900;

    /// <summary>Por debajo de este ancho (px) se oculta el porcentaje y las tarjetas pasan a dos columnas.</summary>
    private const double NarrowMaxWidth = 600;

    private MainWindowViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => AttachViewModel();
    }

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

    private void AttachViewModel()
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = DataContext as MainWindowViewModel;

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedNode) && _viewModel?.SelectedNode is { } node)
        {
            _ = BringIntoViewAsync(node);
        }
    }

    /// <summary>
    /// Desplaza el árbol hasta la fila seleccionada. Su contenedor no existe hasta el siguiente pase de
    /// diseño (y, en carpetas grandes, hasta que llegan sus filas), así que se reintenta unos instantes.
    /// </summary>
    private async Task BringIntoViewAsync(NodeViewModel node)
    {
        for (var attempt = 0; attempt < 40; attempt++)
        {
            if (ResultTree.TreeContainerFromItem(node) is { } container)
            {
                container.BringIntoView();
                return;
            }

            await Task.Delay(50);

            if (!ReferenceEquals(_viewModel?.SelectedNode, node))
            {
                return;
            }
        }
    }
}
