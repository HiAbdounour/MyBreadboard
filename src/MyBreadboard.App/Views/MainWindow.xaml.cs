using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MyBreadboard.App.Views;

public partial class MainWindow : Window
{
    private FrameworkElement? _selectedElement;
    private FrameworkElement? _draggedElement;
    private Point _dragStart;
    private double _dragOriginLeft;
    private double _dragOriginTop;

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

        border.SetValue(Canvas.LeftProperty, x);
        border.SetValue(Canvas.TopProperty, y);
        border.MouseLeftButtonDown += Component_MouseLeftButtonDown;

        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var text = new TextBlock
        {
            Text = type,
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.Black,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };

        stack.Children.Add(text);
        border.Child = stack;

        DrawingSurface.Children.Add(border);
        SelectElement(border);
    }

    private void Component_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element)
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
        if (e.Source == DrawingSurface)
        {
            SelectElement(null);
        }
    }

    private void DrawingSurface_MouseMove(object sender, MouseEventArgs e)
    {
        if (_draggedElement is null || !DrawingSurface.IsMouseCaptured)
        {
            return;
        }

        var currentPosition = e.GetPosition(DrawingSurface);
        var deltaX = currentPosition.X - _dragStart.X;
        var deltaY = currentPosition.Y - _dragStart.Y;

        var newLeft = _dragOriginLeft + deltaX;
        var newTop = _dragOriginTop + deltaY;

        Canvas.SetLeft(_draggedElement, Math.Max(0, newLeft));
        Canvas.SetTop(_draggedElement, Math.Max(0, newTop));
    }

    private void DrawingSurface_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_draggedElement is not null)
        {
            _draggedElement = null;
            DrawingSurface.ReleaseMouseCapture();
        }
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
            border.BorderBrush = Brushes.DeepPurple;
            border.BorderThickness = new Thickness(3);
            return;
        }

        if (element is Shape shape)
        {
            shape.Stroke = Brushes.DeepPurple;
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
            StrokeThickness = 2
        };
        Canvas.SetLeft(sourceRect, 200);
        Canvas.SetTop(sourceRect, 200);
        sourceRect.MouseLeftButtonDown += Component_MouseLeftButtonDown;
        DrawingSurface.Children.Add(sourceRect);

        var loadRect = new Rectangle
        {
            Width = 120,
            Height = 80,
            Fill = Brushes.White,
            Stroke = Brushes.Black,
            StrokeThickness = 2
        };
        Canvas.SetLeft(loadRect, 700);
        Canvas.SetTop(loadRect, 300);
        loadRect.MouseLeftButtonDown += Component_MouseLeftButtonDown;
        DrawingSurface.Children.Add(loadRect);

        DrawingSurface.Children.Add(new Line
        {
            X1 = 320,
            Y1 = 240,
            X2 = 500,
            Y2 = 240,
            Stroke = Brushes.Black,
            StrokeThickness = 2
        });
        DrawingSurface.Children.Add(new Line
        {
            X1 = 500,
            Y1 = 240,
            X2 = 500,
            Y2 = 340,
            Stroke = Brushes.Black,
            StrokeThickness = 2
        });
        DrawingSurface.Children.Add(new Line
        {
            X1 = 500,
            Y1 = 340,
            X2 = 700,
            Y2 = 340,
            Stroke = Brushes.Black,
            StrokeThickness = 2
        });

        var annotation = new TextBlock
        {
            Text = "Schéma de test",
            FontSize = 24,
            Foreground = Brushes.Black
        };
        Canvas.SetLeft(annotation, 420);
        Canvas.SetTop(annotation, 120);
        DrawingSurface.Children.Add(annotation);
    }
}
