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
    private Point _lastWirePointer;
    private double _dragOriginLeft;
    private double _dragOriginTop;
    private bool _isWireMode;

    public MainWindow()
    {
        InitializeComponent();
        AddDemoContent();
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
        EditorStatus.Text = enabled
            ? "Mode fil actif : maintenez le clic gauche et déplacez la souris. Les segments restent horizontaux ou verticaux. Appuyez sur W pour quitter."
            : "Sélection : cliquez sur un objet. Fil : appuyez sur W puis dessinez en maintenant le clic gauche.";
        Mouse.OverrideCursor = enabled ? Cursors.Pen : null;
        SelectElement(null);
    }

    private void AddComponentToCanvas(string type, double x, double y)
    {
        var border = new Border
        {
            Width = 120,
            Height = 80,
            Background = Brushes.White,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(8),
            Tag = type,
            Padding = new Thickness(8)
        };

        Canvas.SetLeft(border, x);
        Canvas.SetTop(border, y);
        border.MouseLeftButtonDown += Component_MouseLeftButtonDown;
        border.MouseRightButtonDown += SelectForContextMenu;
        border.ContextMenu = CreateObjectContextMenu();

        border.Child = new TextBlock
        {
            Text = type,
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.Black,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };

        DrawingSurface.Children.Add(border);
        SelectElement(border);
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
            StartWire(e.GetPosition(DrawingSurface));
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
        _lastWirePointer = startPoint;
        DrawingSurface.CaptureMouse();
    }

    private void DrawingSurface_MouseMove(object sender, MouseEventArgs e)
    {
        if (_activeWire is not null && DrawingSurface.IsMouseCaptured)
        {
            ExtendOrthogonalWire(e.GetPosition(DrawingSurface));
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

    private void ExtendOrthogonalWire(Point pointer)
    {
        if (_activeWire is null)
        {
            return;
        }

        var deltaX = pointer.X - _lastWirePointer.X;
        var deltaY = pointer.Y - _lastWirePointer.Y;
        var lastPoint = _activeWire.Points[^1];

        // For each mouse movement, follow its dominant axis. Every stored segment
        // therefore has either a constant X or a constant Y; diagonal segments are impossible.
        if (Math.Abs(deltaX) >= Math.Abs(deltaY))
        {
            _activeWire.Points[^1] = new Point(pointer.X, lastPoint.Y);
        }
        else
        {
            _activeWire.Points[^1] = new Point(lastPoint.X, pointer.Y);
        }

        _lastWirePointer = pointer;
    }

    private void DrawingSurface_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_activeWire is not null)
        {
            var finalPointer = e.GetPosition(DrawingSurface);
            ExtendOrthogonalWire(finalPointer);
            _activeWire = null;
            DrawingSurface.ReleaseMouseCapture();
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
        if (_isWireMode && _activeWire is not null)
        {
            DrawingSurface.Children.Remove(_activeWire);
            _activeWire = null;
            DrawingSurface.ReleaseMouseCapture();
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

        if (ReferenceEquals(_selectedElement, _activeWire))
        {
            _activeWire = null;
        }

        DrawingSurface.Children.Remove(_selectedElement);
        _selectedElement = null;
    }

    private void SelectElement(FrameworkElement? element)
    {
        if (_selectedElement is Border selectedBorder && selectedBorder != element)
        {
            selectedBorder.BorderBrush = Brushes.Black;
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

    private void AddDemoContent()
    {
        var sourceRect = new Rectangle
        {
            Width = 120,
            Height = 80,
            Fill = Brushes.White,
            Stroke = Brushes.Black,
            StrokeThickness = 2,
            Tag = "Générateur de tension"
        };
        Canvas.SetLeft(sourceRect, 200);
        Canvas.SetTop(sourceRect, 200);
        sourceRect.MouseLeftButtonDown += Component_MouseLeftButtonDown;
        sourceRect.MouseRightButtonDown += SelectForContextMenu;
        sourceRect.ContextMenu = CreateObjectContextMenu();
        DrawingSurface.Children.Add(sourceRect);

        var loadRect = new Rectangle
        {
            Width = 120,
            Height = 80,
            Fill = Brushes.White,
            Stroke = Brushes.Black,
            StrokeThickness = 2,
            Tag = "Résistance"
        };
        Canvas.SetLeft(loadRect, 700);
        Canvas.SetTop(loadRect, 300);
        loadRect.MouseLeftButtonDown += Component_MouseLeftButtonDown;
        loadRect.MouseRightButtonDown += SelectForContextMenu;
        loadRect.ContextMenu = CreateObjectContextMenu();
        DrawingSurface.Children.Add(loadRect);

        DrawingSurface.Children.Add(new Line
        {
            X1 = 320, Y1 = 240, X2 = 500, Y2 = 240,
            Stroke = Brushes.Black, StrokeThickness = 2,
            IsHitTestVisible = false
        });
        DrawingSurface.Children.Add(new Line
        {
            X1 = 500, Y1 = 240, X2 = 500, Y2 = 340,
            Stroke = Brushes.Black, StrokeThickness = 2,
            IsHitTestVisible = false
        });
        DrawingSurface.Children.Add(new Line
        {
            X1 = 500, Y1 = 340, X2 = 700, Y2 = 340,
            Stroke = Brushes.Black, StrokeThickness = 2,
            IsHitTestVisible = false
        });

        var annotation = new TextBlock
        {
            Text = "Schéma de test",
            FontSize = 24,
            Foreground = Brushes.Black,
            Tag = "Annotation",
            Background = Brushes.Transparent
        };
        Canvas.SetLeft(annotation, 420);
        Canvas.SetTop(annotation, 120);
        annotation.MouseLeftButtonDown += Component_MouseLeftButtonDown;
        annotation.MouseRightButtonDown += SelectForContextMenu;
        annotation.ContextMenu = CreateObjectContextMenu();
        DrawingSurface.Children.Add(annotation);
    }
}
