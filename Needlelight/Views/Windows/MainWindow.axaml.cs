using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Needlelight.Views.Windows;

public partial class MainWindow : Window
{
    private IDisposable? _windowStateSubscription;
    private Path? _maximizeIconPath;
    private Button? _maximizeButton;

    public MainWindow()
    {
        InitializeComponent();

        _maximizeIconPath = this.FindControl<Path>("MaximizeIconPath");
        _maximizeButton = this.FindControl<Button>("MaximizeButton");

        _windowStateSubscription = this.GetObservable<WindowState>(WindowStateProperty)
            .Subscribe(_ => UpdateWindowStateVisuals());

        UpdateWindowStateVisuals();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (WindowState != WindowState.Maximized)
        {
            WindowState = WindowState.Maximized;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _windowStateSubscription?.Dispose();
    }

    private void MinimizeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeRestoreButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ToggleWindowState();
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void TopBar_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (IsInteractiveElement(e.Source))
        {
            return;
        }

        if (e.ClickCount > 1)
        {
            return;
        }

        try
        {
            BeginMoveDrag(e);
            e.Handled = true;
        }
        catch (InvalidOperationException)
        {
            // Ignore if dragging cannot be initiated (e.g., platform restrictions).
        }
    }

    private void TopBar_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (IsInteractiveElement(e.Source))
        {
            return;
        }

        ToggleWindowState();
        e.Handled = true;
    }

    private void ToggleWindowState()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void UpdateWindowStateVisuals()
    {
        if (_maximizeIconPath is null)
        {
            return;
        }

        var resourceKey = WindowState == WindowState.Maximized
            ? "Icon.Restore"
            : "Icon.Maximize";

        if (this.TryFindResource(resourceKey, out var resource) && resource is StreamGeometry streamGeometry)
        {
            _maximizeIconPath.Data = streamGeometry;
            _maximizeIconPath.Fill = Brushes.Transparent;
            _maximizeIconPath.StrokeThickness = WindowState == WindowState.Maximized ? 1.6 : 1.4;
        }

        if (_maximizeButton is not null)
        {
            var tip = WindowState == WindowState.Maximized ? "Restore" : "Maximize";
            ToolTip.SetTip(_maximizeButton, tip);
        }
    }

    private static bool IsInteractiveElement(object? source)
    {
        if (source is not Control control)
        {
            return false;
        }

        static bool IsInteractive(Control element) =>
            element.Classes.Contains("window-control") ||
            element is Button ||
            element is ToggleButton ||
            element is ComboBox ||
            element is ListBoxItem ||
            element is TextBox;

        if (IsInteractive(control))
        {
            return true;
        }

        return control
            .GetVisualAncestors()
            .OfType<Control>()
            .Any(IsInteractive);
    }
}

