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
    private const double WireThickness = 6;

    private FrameworkElement? _selectedElement;
    private FrameworkElement? _draggedElement;
    private Polyline? _activeWire;
    private Point _dragStart;
    private Point _wireStart;
    private double _dragOriginLeft;
    private double _dragOriginTop;
    private Orientation? _wireOrientation;
    private bool _isWireMode;
    private Brush? _selectedOriginalBorderBrush;
    private Thickness _selectedOriginalBorderThickness;
    private Brush? _selectedOriginalStroke;
    private double _selectedOriginalStrokeThickness;

    public MainWindow()
    {
        InitializeComponent();
        AddSvgComponentsToPalette();
    }

    private void AddSvgComponentsToPalette()
    {
        if (Content is not Grid root)
        {
            return;
        }

        var palette = root.Children
            .OfType<Border>()
            .FirstOrDefault(border => Grid.GetRow(border) == 1)?.Child as WrapPanel;
        if (palette is null)
        {
            return;
        }

        string[] names =
        [
            "Masse",
            "Boîte noire",
            "Cellule photovoltaïque",
            "Photodiode",
            "Moteur",
            "Potentiomètre",
            "Résistance variable"
        ];

        foreach (var name in names)
        {
            var button = new Button
            {
                Style = (Style)FindResource("PaletteButtonStyle"),
                Tag = name,
                ToolTip = name
            };
            var content = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            content.Children.Add(new Viewbox
            {
                Width = 86,
                Height = 58,
                Stretch = Stretch.Uniform,
                Child = CreateSvgComponentVisual(name)
            });
            content.Children.Add(new TextBlock
            {
                Text = name,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                FontSize = 11
            });
            button.Content = content;
            button.Click += AddComponentButton_Click;
            palette.Children.Add(button);
        }
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
        if (type is "Masse" or "Boîte noire" or "Cellule photovoltaïque" or "Photodiode" or
            "Moteur" or "Potentiomètre" or "Résistance variable")
        {
            return CreateSvgComponentVisual(type);
        }

        if (type == "Annotation")
        {
            return CreateAnnotationTextBlock("T");
        }

        var geometry = type switch
        {
            "Générateur de tension" => "M2,21 H24 M62,21 H84 M24,21 A19,19 0 1 1 62,21 A19,19 0 1 1 24,21 M36,12 L51,21 L36,30",
            "Générateur de courant" => "M2,21 H18 M48,21 H84 M18,21 A19,19 0 1 1 56,21 A19,19 0 1 1 18,21 M30,21 A19,21 0 1 1 68,21 A19,21 0 1 1 30,21 M38,12 L53,21 L38,30",
            "Résistance" => "M2,21 H22 M22,9 H64 V33 H22 Z M64,21 H84",
            "Interrupteur" => "M2,21 H25 M61,21 H84 M25,21 A4,4 0 1 1 25,20.9 M61,21 A4,4 0 1 1 61,20.9 M27,20 L59,20",
            "Diode" => "M2,21 H23 M23,8 L47,21 L23,34 Z M51,8 V34 M51,21 H84",
            "LED" => "M2,25 H23 M23,12 L47,25 L23,38 Z M51,12 V38 M51,25 H78 M53,9 L66,-3 M64,13 L77,1",
            "Bouton poussoir" => "M2,12 H16 M70,12 H84 M2,32 H16 M70,32 H84 M16,4 H70 V40 H16 Z M31,12 L42,23 M55,32 L44,21 M31,12 V17 M55,27 V32",
            _ => "M4,21 H82"
        };

        var symbolCanvas = new Canvas { Width = 86, Height = 42 };
        symbolCanvas.Children.Add(new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse(geometry),
            Stroke = Brushes.Black,
            StrokeThickness = 3.5,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            Fill = type is "Diode" or "LED" ? Brushes.White : Brushes.Transparent
        });

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

    private static FrameworkElement CreateSvgComponentVisual(string type)
    {
        var canvas = new Canvas { Width = 800, Height = 600 };

        switch (type)
        {
            case "Masse":
                AddSvgLine(canvas, 311.80043, 268.00003, 488.19958, 268);
                AddSvgLine(canvas, 347.3006, 286, 452.69942, 285);
                AddSvgLine(canvas, 370.8007, 302.99999, 429.1993, 301.99999);
                AddSvgLine(canvas, 398, 92, 398, 266.00264);
                break;
            case "Boîte noire":
                canvas.Children.Add(new Rectangle
                {
                    Width = 291.00136,
                    Height = 431.00201,
                    Left = 224.00289,
                    Top = 90.00182,
                    Fill = Brushes.White,
                    Stroke = Brushes.Black,
                    StrokeThickness = 10
                });
                break;
            case "Cellule photovoltaïque":
                AddSvgLine(canvas, 203, 261, 553.39998, 260);
                AddSvgRectangle(canvas, 246.00299, 200.00233, 261.00122, 114.00053);
                AddSolarRay(canvas, 308.77512, 129.64132, 432.7757, 130.64132, 370.775, 130.141);
                AddSolarRay(canvas, 374.33422, 185.50245, 340.33406, 158.50232, 357.334, 172.002);
                AddSolarRay(canvas, 350.13459, 148.6203, 320.13445, 171.62041, 335.135, 160.12);
                AddSolarRay(canvas, 384.77491, 137.64168, 508.77549, 138.64168, 446.775, 138.142);
                AddSolarRay(canvas, 450.33391, 193.50286, 416.33375, 166.50273, 433.334, 180.003);
                AddSolarRay(canvas, 426.13428, 156.62071, 396.13414, 179.62082, 411.134, 168.121);
                break;
            case "Photodiode":
                AddSvgLine(canvas, 203, 261, 553.39998, 260);
                canvas.Children.Add(new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M292.19824,338.00073 L385.19869,182 L478.19914,338.00073 Z"),
                    Fill = Brushes.White,
                    Stroke = Brushes.Black,
                    StrokeThickness = 10,
                    RenderTransform = new RotateTransform(90, 385.199, 260)
                });
                AddSvgLine(canvas, 463.004, 160.00215, 462.004, 364.0031);
                AddPhotodiodeRay(canvas, 336.00341, 112.00191, 460.00399, 113.00192,
                    423.00381, 86.00179, 457.00397, 113.00192,
                    428.00384, 136.00202, 458.00398, 113.00192, 398.004, 111.002);
                AddPhotodiodeRay(canvas, 390.00366, 135.00202, 514.00424, 136.00202,
                    477.00407, 109.0019, 511.00423, 136.00202,
                    482.00409, 159.00213, 512.00423, 136.00202, 452.004, 134.002);
                break;
            case "Moteur":
                AddSvgLine(canvas, 203, 261, 553.39998, 260);
                canvas.Children.Add(new Ellipse
                {
                    Width = 184,
                    Height = 178,
                    Left = 281,
                    Top = 171,
                    Fill = Brushes.White,
                    Stroke = Brushes.Black,
                    StrokeThickness = 10
                });
                canvas.Children.Add(new TextBlock
                {
                    Text = "M",
                    FontFamily = new FontFamily("Alumni Sans"),
                    FontWeight = FontWeights.Bold,
                    FontSize = 120,
                    Foreground = Brushes.Black,
                    Left = 343.39999,
                    Top = 173.4
                });
                break;
            case "Potentiomètre":
                AddSvgLine(canvas, 203, 261, 553.39998, 260);
                AddSvgRectangle(canvas, 249.39999, 219, 251, 82);
                AddSvgLine(canvas, 375.39999, 301.4, 375, 337.4);
                break;
            case "Résistance variable":
                AddSvgLine(canvas, 203, 261, 553.39998, 260);
                AddSvgRectangle(canvas, 249.39999, 219, 251, 82);
                AddSvgLine(canvas, 246.39999, 347.4, 498.39999, 163.4);
                AddSvgPath(canvas, "M500.39999,163.4 L462.39999,163.4");
                AddSvgPath(canvas, "M514.39999,181.4 L476.39999,181.4",
                    new RotateTransform(90, 495.4, 181.4));
                break;
        }

        return new Viewbox
        {
            Stretch = Stretch.Uniform,
            Margin = new Thickness(4),
            Child = canvas
        };
    }

    private static void AddSvgLine(Canvas canvas, double x1, double y1, double x2, double y2)
    {
        canvas.Children.Add(new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = Brushes.Black,
            StrokeThickness = 10
        });
    }

    private static void AddSvgRectangle(Canvas canvas, double left, double top, double width, double height)
    {
        canvas.Children.Add(new Rectangle
        {
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            Fill = Brushes.White,
            Stroke = Brushes.Black,
            StrokeThickness = 10
        });
    }

    private static void AddSvgPath(Canvas canvas, string geometry, Transform? transform = null)
    {
        canvas.Children.Add(new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse(geometry),
            Stroke = Brushes.Black,
            StrokeThickness = 10,
            RenderTransform = transform
        });
    }

    private static void AddSolarRay(Canvas canvas, double x1, double y1, double x2, double y2,
        double centerX, double centerY)
    {
        var geometry = new GeometryGroup
        {
            Transform = new RotateTransform(125, centerX, centerY)
        };
        geometry.Children.Add(new LineGeometry(new Point(x1, y1), new Point(x2, y2)));
        AddSvgPath(canvas, geometry);
    }

    private static void AddPhotodiodeRay(Canvas canvas,
        double rayX1, double rayY1, double rayX2, double rayY2,
        double arrowOneX1, double arrowOneY1, double arrowOneX2, double arrowOneY2,
        double arrowTwoX1, double arrowTwoY1, double arrowTwoX2, double arrowTwoY2,
        double centerX, double centerY)
    {
        var geometry = new GeometryGroup
        {
            Transform = new RotateTransform(125, centerX, centerY)
        };
        geometry.Children.Add(new LineGeometry(new Point(rayX1, rayY1), new Point(rayX2, rayY2)));
        geometry.Children.Add(new LineGeometry(new Point(arrowOneX1, arrowOneY1), new Point(arrowOneX2, arrowOneY2)));
        geometry.Children.Add(new LineGeometry(new Point(arrowTwoX1, arrowTwoY1), new Point(arrowTwoX2, arrowTwoY2)));
        AddSvgPath(canvas, geometry);
    }

    private static void AddSvgPath(Canvas canvas, Geometry geometry)
    {
        canvas.Children.Add(new System.Windows.Shapes.Path
        {
            Data = geometry,
            Stroke = Brushes.Black,
            StrokeThickness = 10
        });
    }

    private static TextBlock CreateAnnotationTextBlock(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontFamily = new FontFamily("Georgia"),
            FontSize = text.Length <= 2 ? 48 : 22,
            Foreground = Brushes.Black,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private void BeginAnnotationEdit(Border annotation)
    {
        if (annotation.Child is not TextBlock currentText)
        {
            return;
        }

        var editor = new TextBox
        {
            Text = currentText.Text,
            FontFamily = currentText.FontFamily,
            FontSize = 22,
            Foreground = Brushes.Black,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(2),
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Tag = annotation,
            ToolTip = currentText.Text
        };
        editor.LostKeyboardFocus += AnnotationEditor_LostKeyboardFocus;
        editor.PreviewKeyDown += AnnotationEditor_PreviewKeyDown;
        annotation.Child = editor;
        editor.Focus();
        editor.SelectAll();
        EditorStatus.Text = "Modification de l’annotation : Entrée pour terminer, Maj+Entrée pour une nouvelle ligne, Échap pour annuler.";
    }

    private void AnnotationEditor_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox editor)
        {
            return;
        }

        if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            FinishAnnotationEdit(editor, commit: true);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            FinishAnnotationEdit(editor, commit: false);
            e.Handled = true;
        }
    }

    private void AnnotationEditor_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox editor)
        {
            FinishAnnotationEdit(editor, commit: true);
        }
    }

    private void FinishAnnotationEdit(TextBox editor, bool commit)
    {
        if (editor.Tag is not Border annotation || !ReferenceEquals(annotation.Child, editor))
        {
            return;
        }

        editor.LostKeyboardFocus -= AnnotationEditor_LostKeyboardFocus;
        editor.PreviewKeyDown -= AnnotationEditor_PreviewKeyDown;
        annotation.Child = CreateAnnotationTextBlock(commit ? editor.Text : editor.ToolTip as string ?? string.Empty);
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
        if (_isWireMode || sender is not Border element)
        {
            return;
        }

        // The text box handles pointer interaction while annotation editing is active.
        if (e.OriginalSource is TextBoxBase)
        {
            return;
        }

        if (string.Equals(element.Tag?.ToString(), "Annotation", StringComparison.Ordinal) && e.ClickCount == 2)
        {
            SelectElement(element);
            BeginAnnotationEdit(element);
            e.Handled = true;
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

    private void Wire_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!_isWireMode && sender is FrameworkElement wire)
        {
            SelectElement(wire);
            e.Handled = true;
        }
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
            StrokeThickness = WireThickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            IsHitTestVisible = true,
            Tag = "Wire",
            ContextMenu = CreateObjectContextMenu()
        };
        _activeWire.Points.Add(startPoint);
        _activeWire.Points.Add(startPoint);
        _activeWire.MouseLeftButtonDown += Wire_MouseLeftButtonDown;
        _activeWire.MouseRightButtonDown += SelectForContextMenu;
        DrawingSurface.Children.Add(_activeWire);
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

        _activeWire.Points[1] = _wireOrientation == Orientation.Horizontal
            ? new Point(pointer.X, _wireStart.Y)
            : new Point(_wireStart.X, pointer.Y);
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
        else if (e.Key == Key.Delete)
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
            _wireOrientation = null;
        }

        DrawingSurface.Children.Remove(_selectedElement);
        _selectedElement = null;
    }

    private void SelectElement(FrameworkElement? element)
    {
        if (ReferenceEquals(_selectedElement, element))
        {
            return;
        }

        RestoreSelectedAppearance();
        _selectedElement = element;

        if (element is Border border)
        {
            _selectedOriginalBorderBrush = border.BorderBrush;
            _selectedOriginalBorderThickness = border.BorderThickness;
            border.BorderBrush = SelectionBrush;
            border.BorderThickness = new Thickness(3);
        }
        else if (element is Shape shape)
        {
            _selectedOriginalStroke = shape.Stroke;
            _selectedOriginalStrokeThickness = shape.StrokeThickness;
            shape.Stroke = SelectionBrush;
            shape.StrokeThickness = Math.Max(shape.StrokeThickness + 2, 8);
        }
    }

    private void RestoreSelectedAppearance()
    {
        if (_selectedElement is Border border)
        {
            border.BorderBrush = _selectedOriginalBorderBrush ?? Brushes.Transparent;
            border.BorderThickness = _selectedOriginalBorderThickness;
        }
        else if (_selectedElement is Shape shape)
        {
            shape.Stroke = _selectedOriginalStroke ?? Brushes.Black;
            shape.StrokeThickness = _selectedOriginalStrokeThickness;
        }

        _selectedOriginalBorderBrush = null;
        _selectedOriginalStroke = null;
    }
}
