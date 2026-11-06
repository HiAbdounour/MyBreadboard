using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MyBreadboard.App.Views;

public partial class MainWindow : Window
{
    private static readonly Brush SelectionBrush = new SolidColorBrush(Color.FromRgb(104, 66, 166));

    private FrameworkElement? _selectedElement;
    private FrameworkElement? _draggedElement;
    private Polyline? _activeWire;
    private Point _dragStart;
    private Point _wireStart;
    private double _dragOriginLeft;
    private double _dragOriginTop;
    private Orientation? _wireOrientation;
    private bool _isWireMode;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void AddComponentButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        var type = button.Tag?.ToString() ?? "Composant";
        var x = 120 + (DrawingSurface.Children.Count * 18) % 500;
        var y = 140 + (DrawingSurface.Children.Count * 20) % 280;
        AddComponentToCanvas(type, x, y);
    }

    private void WireToolButton_Click(object sender, RoutedEventArgs e)
    {
        SetWireMode(!_isWireMode);
    }

    private void SetWireMode(bool enabled)
    {
        _isWireMode = enabled;
        if (!enabled)
        {
            // Leaving wire mode keeps any completed portion of the current stroke.
            FinishActiveWire();
        }

        EditorStatus.Text = enabled
            ? "Mode fil actif : cliquez pour commencer un fil, déplacez la souris pour choisir son axe, puis cliquez à nouveau pour le terminer. W pour quitter."
            : "Sélection : cliquez sur un objet. Fil : appuyez sur W pour activer le mode.";
        Mouse.OverrideCursor = enabled ? Cursors.Pen : null;
        SelectElement(null);
    }

    private void AddComponentToCanvas(string type, double x, double y)
    {
        var border = new Border
        {
            Width = 120,
            Height = 72,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(2),
            Tag = type,
            ToolTip = type,
            Child = CreateComponentVisual(type)
        };

        Canvas.SetLeft(border, x);
        Canvas.SetTop(border, y);
        border.MouseLeftButtonDown += Component_MouseLeftButtonDown;
        border.MouseRightButtonDown += SelectForContextMenu;
        border.ContextMenu = CreateObjectContextMenu();

        DrawingSurface.Children.Add(border);
        SelectElement(border);
    }

    private static FrameworkElement CreateComponentVisual(string type)
    {
        if (type == "Annotation")
        {
            return new TextBlock
            {
                Text = "T",
                FontFamily = new FontFamily("Georgia"),
                FontSize = 48,
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        var geometry = type switch
        {
            "Générateur de tension" => "M2,21 H24 M62,21 H84 M24,21 A19,19 0 1 1 62,21 A19,19 0 1 1 24,21 M36,12 L51,21 L36,30",
            "Générateur de courant" => "M2,21 H18 M48,21 H84 M18,21 A19,19 0 1 1 56,21 A19,19 0 1 1 18,21 M30,21 A19,19 0 1 1 68,21 A19,19 0 1 1 30,21 M38,12 L53,21 L38,30",
            "Résistance" => "M2,21 H22 M22,9 H64 V33 H22 Z M64,21 H84",
            "Interrupteur" => "M2,21 H25 M61,21 H84 M25,21 A4,4 0 1 1 25,20.9 M61,21 A4,4 0 1 1 61,20.9 M27,20 L59,20",
            "Diode" => "M2,21 H23 M23,8 L47,21 L23,34 Z M51,8 V34 M51,21 H84",
            "LED" => "M2,25 H23 M23,12 L47,25 L23,38 Z M51,12 V38 M51,25 H78 M53,9 L66,-3 M64,13 L77,1",
            "Bouton poussoir" => "M2,12 H16 M70,12 H84 M2,32 H16 M70,32 H84 M16,4 H70 V40 H16 Z M31,12 L42,23 M55,32 L44,21 M31,12 V17 M55,27 V32",
            _ => "M4,21 H82"
        };

        var symbolCanvas = new Canvas { Width = 86, Height = 42 };
        var symbol = new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse(geometry),
            Stroke = Brushes.Black,
            StrokeThickness = 3.5,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            Fill = type is "Diode" or "LED" ? Brushes.White : Brushes.Transparent
        };
        symbolCanvas.Children.Add(symbol);

        if (type == "LED")
        {
            symbolCanvas.Children.Add(new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M63,0 L66,-3 L65,2 M74,4 L77,1 L76,6"),
                Fill = Brushes.Black
            });
        }

        return new Viewbox
        {
            Stretch = Stretch.Uniform,
            Margin = new Thickness(4),
            Child = symbolCanvas
        };
    }

    private ContextMenu CreateObjectContextMenu()
    {
        var contextMenu = new ContextMenu
        {
            Background = new SolidColorBrush(Color.FromRgb(42, 42, 42)),
            Foreground = Brushes.White
        };
        var deleteItem = new MenuItem { Header = "Supprimer" };
        deleteItem.Click += DeleteSelected_Click;
        contextMenu.Items.Add(deleteItem);
        return contextMenu;
    }

    private void SelectForContextMenu(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            SelectElement(element);
        }
    }

    private void Component_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_isWireMode || sender is not FrameworkElement element)
        {
            return;
        }

        SelectElement(element);
        _draggedElement = element;
        _dragStart = e.GetPosition(DrawingSurface);
        _dragOriginLeft = Canvas.GetLeft(element);
        _dragOriginTop = Canvas.GetTop(element);

        DrawingSurface.CaptureMouse();
        e.Handled = true;
    }

    private void DrawingSurface_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_isWireMode)
        {
            var position = e.GetPosition(DrawingSurface);
            if (_activeWire is null)
            {
                StartWire(position);
            }
            else
            {
                UpdateActiveWire(position);
                FinishActiveWire();
            }

            e.Handled = true;
            return;
        }

        if (e.Source == DrawingSurface)
        {
            SelectElement(null);
        }
    }

    private void StartWire(Point startPoint)
    {
        _wireStart = startPoint;
        _wireOrientation = null;
        _activeWire = new Polyline
        {
            Stroke = Brushes.Black,
            StrokeThickness = 2,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            IsHitTestVisible = false
        };
        _activeWire.Points.Add(startPoint);
        _activeWire.Points.Add(startPoint);
        DrawingSurface.Children.Add(_activeWire);

        // Capture between clicks so the active wire continues to follow the pointer
        // after the initial click has been released.
        DrawingSurface.CaptureMouse();
        EditorStatus.Text = "Fil en cours : déplacez la souris, puis cliquez pour terminer ce fil.";
    }

    private void DrawingSurface_MouseMove(object sender, MouseEventArgs e)
    {
        if (_activeWire is not null && DrawingSurface.IsMouseCaptured)
        {
            UpdateActiveWire(e.GetPosition(DrawingSurface));
            return;
        }

        if (_draggedElement is null || !DrawingSurface.IsMouseCaptured)
        {
            return;
        }

        var currentPosition = e.GetPosition(DrawingSurface);
        var deltaX = currentPosition.X - _dragStart.X;
        var deltaY = currentPosition.Y - _dragStart.Y;
        var maxLeft = DrawingSurface.Width - _draggedElement.ActualWidth;
        var maxTop = DrawingSurface.Height - _draggedElement.ActualHeight;

        Canvas.SetLeft(_draggedElement, Math.Clamp(_dragOriginLeft + deltaX, 0, maxLeft));
        Canvas.SetTop(_draggedElement, Math.Clamp(_dragOriginTop + deltaY, 0, maxTop));
    }

    private void UpdateActiveWire(Point pointer)
    {
        if (_activeWire is null)
        {
            return;
        }

        var deltaX = pointer.X - _wireStart.X;
        var deltaY = pointer.Y - _wireStart.Y;

        // The first meaningful movement locks the wire to one axis for its whole length.
        if (_wireOrientation is null && (Math.Abs(deltaX) > 0.1 || Math.Abs(deltaY) > 0.1))
        {
            _wireOrientation = Math.Abs(deltaX) >= Math.Abs(deltaY)
                ? Orientation.Horizontal
                : Orientation.Vertical;
        }

        if (_wireOrientation is null)
        {
            return;
        }

        var endpoint = _wireOrientation == Orientation.Horizontal
            ? new Point(pointer.X, _wireStart.Y)
            : new Point(_wireStart.X, pointer.Y);

        // Only one coordinate changes: the rendered segment can never be diagonal.
        _activeWire.Points[1] = endpoint;
    }

    private void FinishActiveWire()
    {
        if (_activeWire is null)
        {
            return;
        }

        _activeWire = null;
        _wireOrientation = null;
        if (DrawingSurface.IsMouseCaptured)
        {
            DrawingSurface.ReleaseMouseCapture();
        }

        if (_isWireMode)
        {
            EditorStatus.Text = "Mode fil actif : cliquez pour commencer un nouveau fil.";
        }
    }

    private void DrawingSurface_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // Wire drawing is click-to-start, move-to-draw, click-to-finish. Releasing
        // the first click must not finish the wire; the next left click does that.
        if (_activeWire is not null)
        {
            UpdateActiveWire(e.GetPosition(DrawingSurface));
            return;
        }

        if (_draggedElement is not null)
        {
            _draggedElement = null;
            DrawingSurface.ReleaseMouseCapture();
        }
    }

    private void DrawingSurface_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_activeWire is not null)
        {
            DrawingSurface.Children.Remove(_activeWire);
            _activeWire = null;
            _wireOrientation = null;
            if (DrawingSurface.IsMouseCaptured)
            {
                DrawingSurface.ReleaseMouseCapture();
            }
            e.Handled = true;
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.FocusedElement is TextBoxBase)
        {
            return;
        }

        if (e.Key == Key.W)
        {
            SetWireMode(!_isWireMode);
            e.Handled = true;
        }
        else if (e.Key == Key.Delete && !_isWireMode)
        {
            DeleteSelected();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && _isWireMode)
        {
            SetWireMode(false);
            e.Handled = true;
        }
    }

    private void DeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        DeleteSelected();
    }

    private void DeleteSelected()
    {
        if (_selectedElement is null)
        {
            return;
        }

        DrawingSurface.Children.Remove(_selectedElement);
        _selectedElement = null;
    }

    private void SelectElement(FrameworkElement? element)
    {
        if (_selectedElement is Border selectedBorder && selectedBorder != element)
        {
            selectedBorder.BorderBrush = Brushes.Transparent;
            selectedBorder.BorderThickness = new Thickness(2);
        }

        if (_selectedElement is Shape selectedShape && selectedShape != element)
        {
            selectedShape.Stroke = Brushes.Black;
            selectedShape.StrokeThickness = 2;
        }

        _selectedElement = element;

        if (element is Border border)
        {
            border.BorderBrush = SelectionBrush;
            border.BorderThickness = new Thickness(3);
        }
        else if (element is Shape shape)
        {
            shape.Stroke = SelectionBrush;
            shape.StrokeThickness = 4;
        }
    }
}
