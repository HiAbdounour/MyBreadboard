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
        var geometry = type switch
        {
            "LED" => "M2,21 H28 M60,21 H84 M28,4 L58,21 L28,38 Z M60,4 V38 M38,10 L53,1 M53,1 L50,8 M53,1 L46,2 M51,17 L66,8 M66,8 L63,15 M66,8 L59,9",
            "Diode" => "M2,21 H28 M28,4 L58,21 L28,38 Z M60,4 V38 M60,21 H84",
            "Bouton poussoir" => "M2,12 H20 M66,12 H84 M2,32 H20 M66,32 H84 M13,6 H72 V36 H13 Z M20,12 L35,32 M50,12 L66,32",
            "Résistance" => "M2,21 H13 M13,11 H72 V31 H13 Z M72,21 H84",
            "Générateur de courant" => "M2,21 H16 M70,21 H84 M21,21 A20,20 0 1 1 61,21 A20,20 0 1 1 21,21 M36,12 L54,22 M54,22 L49,15 M54,22 L46,23",
            "Générateur de tension" => "M2,21 H23 M63,21 H84 M23,21 A20,20 0 1 1 63,21 A20,20 0 1 1 23,21 M35,12 L52,22 M52,22 L47,15 M52,22 L44,23",
            "Interrupteur" => "M2,21 H25 M61,21 H84 M25,21 A4,4 0 1 1 25,20.9 M61,21 A4,4 0 1 1 61,20.9 M27,20 L59,20",
            "Masse" => "M43,3 V21 M20,21 H66 M26,27 H60 M33,33 H53",
            "Boîte noire" => "M12,2 H74 V40 H12 Z",
            "Cellule photovoltaïque" => "M2,21 H12 M74,21 H84 M12,14 H74 V34 H12 Z M25,3 L34,12 M34,12 L33,7 M34,12 L29,11 M52,3 L61,12 M61,12 L56,11 M61,12 L60,7",
            "Photodiode" => "M2,21 H23 M23,8 L47,21 L23,34 Z M51,8 V34 M51,21 H84 M32,3 L41,12 M41,12 L36,11 M41,12 L40,7 M49,3 L58,12 M58,12 L53,11 M58,12 L57,7",
            "Moteur" => "M2,21 H21 M63,21 H84 M42,0 A21,21 0 1 1 42,42 A21,21 0 1 1 42,0 M34,28 V14 L42,23 L50,14 V28",
            "Potentiomètre" => "M2,21 H22 M22,9 H64 V33 H22 Z M64,21 H84 M43,33 V40",
            "Résistance variable" => "M2,21 H22 M22,9 H64 V33 H22 Z M64,21 H84 M12,40 L72,0 M72,0 L62,0 M72,0 L72,10",
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
