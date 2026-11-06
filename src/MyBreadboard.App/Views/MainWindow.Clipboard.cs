using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MyBreadboard.App.Views;

public partial class MainWindow
{
    private const double ClipboardPasteOffset = 24;
    private List<ClipboardItem>? _clipboardItems;
    private List<FrameworkElement> _selectAllItems = new();
    private FrameworkElement? _selectAllAnchor;
    private int _pasteCount;
    private bool _editMenuHandlersAttached;

    static MainWindow()
    {
        EventManager.RegisterClassHandler(typeof(MainWindow), FrameworkElement.LoadedEvent,
            new RoutedEventHandler(MainWindow_LoadedForEditCommands));
    }

    private static void MainWindow_LoadedForEditCommands(object sender, RoutedEventArgs e)
    {
        if (sender is MainWindow window) window.AttachEditMenuHandlers();
    }

    private void AttachEditMenuHandlers()
    {
        if (_editMenuHandlersAttached || Content is not Grid root) return;

        var editMenu = root.Children.OfType<Menu>()
            .SelectMany(menu => menu.Items.OfType<MenuItem>())
            .FirstOrDefault(item => string.Equals(item.Header?.ToString(), "Édition", StringComparison.Ordinal));
        if (editMenu is null) return;

        foreach (var item in editMenu.Items.OfType<MenuItem>())
        {
            switch (item.Header?.ToString())
            {
                case "Copier":
                    item.Click += (_, _) => CopySelection();
                    item.InputGestureText = "Ctrl+C";
                    break;
                case "Coller":
                    item.Click += (_, _) => PasteSelection();
                    item.InputGestureText = "Ctrl+V";
                    break;
                case "Tout sélectionner":
                    item.Click += (_, _) => SelectAllObjects();
                    item.InputGestureText = "Ctrl+A";
                    break;
            }
        }

        _editMenuHandlersAttached = true;
    }

    private void SelectAllObjects()
    {
        _selectAllItems = DrawingSurface.Children
            .OfType<FrameworkElement>()
            .Where(item => item is Border || item is Polyline wire &&
                string.Equals(wire.Tag?.ToString(), "Wire", StringComparison.Ordinal))
            .Where(item => !ReferenceEquals(item, _activeWire))
            .ToList();
        _selectAllAnchor = _selectedElement;
        EditorStatus.Text = $"{_selectAllItems.Count} objet(s) sélectionné(s).";
    }

    private void CopySelection()
    {
        var selection = HasCurrentSelectAllSelection()
            ? _selectAllItems
            : _selectedElement is null ? new List<FrameworkElement>() : new List<FrameworkElement> { _selectedElement };

        if (selection.Count == 0)
        {
            EditorStatus.Text = "Sélectionnez un ou plusieurs objets à copier.";
            return;
        }

        _clipboardItems = selection.Select(CaptureClipboardItem).ToList();
        _pasteCount = 0;
        EditorStatus.Text = $"{_clipboardItems.Count} objet(s) copié(s).";
    }

    private ClipboardItem CaptureClipboardItem(FrameworkElement element)
    {
        if (element is Border component)
        {
            var type = component.Tag?.ToString() ?? string.Empty;
            var text = type == "Annotation"
                ? component.Child switch
                {
                    TextBlock textBlock => textBlock.Text,
                    TextBox textBox => textBox.Text,
                    _ => string.Empty
                }
                : null;

            return new ClipboardItem
            {
                Type = ClipboardItemType.Component,
                ComponentType = type,
                X = Canvas.GetLeft(component),
                Y = Canvas.GetTop(component),
                Width = component.Width,
                Height = component.Height,
                Rotation = (component.RenderTransform as RotateTransform)?.Angle ?? 0,
                Text = text
            };
        }

        if (element is Polyline wire)
        {
            return new ClipboardItem
            {
                Type = ClipboardItemType.Wire,
                Thickness = wire.StrokeThickness,
                Points = wire.Points.Select(point => new ClipboardPoint(point.X, point.Y)).ToList()
            };
        }

        throw new InvalidOperationException("Type d'objet non pris en charge pour la copie.");
    }

    private void PasteSelection()
    {
        if (_clipboardItems is not { Count: > 0 })
        {
            EditorStatus.Text = "Le presse-papiers est vide.";
            return;
        }

        SelectElement(null);
        _selectAllItems.Clear();
        _selectAllAnchor = null;
        var offset = ClipboardPasteOffset * ++_pasteCount;

        foreach (var item in _clipboardItems)
        {
            if (item.Type == ClipboardItemType.Component)
            {
                AddComponentToCanvas(item.ComponentType!, item.X + offset, item.Y + offset);
                var component = (Border)DrawingSurface.Children[^1];
                component.Width = item.Width;
                component.Height = item.Height;
                component.RenderTransformOrigin = new Point(0.5, 0.5);
                component.RenderTransform = new RotateTransform(NormalizeAngle(item.Rotation));
                if (item.ComponentType == "Annotation" && component.Child is TextBlock annotation)
                {
                    annotation.Text = item.Text ?? "T";
                }
            }
            else
            {
                var wire = new Polyline
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = item.Thickness,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    StrokeLineJoin = PenLineJoin.Round,
                    IsHitTestVisible = true,
                    Tag = "Wire",
                    ContextMenu = CreateObjectContextMenu()
                };
                foreach (var point in item.Points!)
                    wire.Points.Add(new Point(point.X + offset, point.Y + offset));
                wire.MouseLeftButtonDown += Wire_MouseLeftButtonDown;
                wire.MouseRightButtonDown += SelectForContextMenu;
                DrawingSurface.Children.Add(wire);
                SelectElement(wire);
            }
        }

        EditorStatus.Text = $"{_clipboardItems.Count} objet(s) collé(s).";
    }

    private bool HasCurrentSelectAllSelection() =>
        _selectAllItems.Count > 0 && ReferenceEquals(_selectedElement, _selectAllAnchor);

    private void DeleteAllSelectedObjects()
    {
        var items = _selectAllItems.Where(DrawingSurface.Children.Contains).ToList();
        if (items.Count == 0) return;

        SelectElement(null);
        foreach (var item in items) DrawingSurface.Children.Remove(item);
        _selectAllItems.Clear();
        _selectAllAnchor = null;
        EditorStatus.Text = $"{items.Count} objet(s) supprimé(s).";
    }

    private enum ClipboardItemType { Component, Wire }

    private sealed class ClipboardItem
    {
        public ClipboardItemType Type { get; init; }
        public string? ComponentType { get; init; }
        public string? Text { get; init; }
        public double X { get; init; }
        public double Y { get; init; }
        public double Width { get; init; }
        public double Height { get; init; }
        public double Rotation { get; init; }
        public double Thickness { get; init; }
        public List<ClipboardPoint>? Points { get; init; }
    }

    private sealed record ClipboardPoint(double X, double Y);
}
