using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using MyBreadboard.App.Models;
using WpfPoint = System.Windows.Point;
using IOPath = System.IO.Path;

namespace MyBreadboard.App.Views;

public partial class MainWindow
{
    private const int CurrentDocumentFormatVersion = 1;
    private const int MaximumDocumentItems = 10000;
    private static readonly JsonSerializerOptions DocumentJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private string? _currentFilePath;
    private bool _fileMenuHandlersAttached;

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        AttachFileMenuHandlers();
    }

    private void AttachFileMenuHandlers()
    {
        if (_fileMenuHandlersAttached || Content is not Grid root)
        {
            return;
        }

        var fileMenu = root.Children
            .OfType<Menu>()
            .SelectMany(menu => menu.Items.OfType<MenuItem>())
            .FirstOrDefault(item => string.Equals(item.Header?.ToString(), "Fichier", StringComparison.Ordinal));

        if (fileMenu is null)
        {
            return;
        }

        foreach (var item in fileMenu.Items.OfType<MenuItem>())
        {
            switch (item.Header?.ToString())
            {
                case "Nouveau":
                    item.Click += NewDocument_Click;
                    break;
                case "Ouvrir":
                    item.Click += OpenDocument_Click;
                    break;
                case "Enregistrer":
                    item.Click += SaveDocument_Click;
                    break;
            }
        }

        _fileMenuHandlersAttached = true;
    }

    private void NewDocument_Click(object sender, RoutedEventArgs e)
    {
        if (_activeWire is not null)
        {
            FinishActiveWire();
        }

        if (DrawingSurface.Children.Count > 0)
        {
            var result = MessageBox.Show(this,
                "Enregistrer le schéma actuel avant de créer un nouveau document ?",
                "Nouveau document",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel ||
                (result == MessageBoxResult.Yes && !SaveDocument()))
            {
                return;
            }
        }

        SelectElement(null);
        DrawingSurface.Children.Clear();
        _selectedElement = null;
        _draggedElement = null;
        _activeWire = null;
        _wireOrientation = null;
        if (DrawingSurface.IsMouseCaptured)
        {
            DrawingSurface.ReleaseMouseCapture();
        }

        _currentFilePath = null;
        UpdateWindowTitle();
        EditorStatus.Text = "Nouveau schéma : placez des composants ou appuyez sur W pour tracer un fil.";
    }

    private void SaveDocument_Click(object sender, RoutedEventArgs e)
    {
        SaveDocument();
    }

    private void OpenDocument_Click(object sender, RoutedEventArgs e)
    {
        OpenDocument();
    }

    private bool SaveDocument()
    {
        if (_activeWire is not null)
        {
            FinishActiveWire();
        }

        var path = _currentFilePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            var dialog = new SaveFileDialog
            {
                Title = "Enregistrer le schéma",
                Filter = "Schéma MyBreadboard (*.mybrd)|*.mybrd",
                DefaultExt = ".mybrd",
                AddExtension = true,
                FileName = "Sans titre.mybrd"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return false;
            }

            path = dialog.FileName;
        }

        try
        {
            var document = CaptureDocument();
            var json = JsonSerializer.Serialize(document, DocumentJsonOptions);
            File.WriteAllText(path, json);
            _currentFilePath = path;
            UpdateWindowTitle();
            EditorStatus.Text = $"Schéma enregistré : {IOPath.GetFileName(path)}";
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            MessageBox.Show(this,
                $"Impossible d'enregistrer le schéma.\n\n{exception.Message}",
                "Erreur d'enregistrement",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
    }

    private BreadboardFile CaptureDocument()
    {
        var document = new BreadboardFile();

        foreach (var component in DrawingSurface.Children.OfType<Border>())
        {
            var type = component.Tag?.ToString();
            if (string.IsNullOrWhiteSpace(type))
            {
                continue;
            }

            document.Components.Add(new BreadboardComponentFile
            {
                Type = type,
                X = Canvas.GetLeft(component),
                Y = Canvas.GetTop(component),
                Width = component.Width,
                Height = component.Height,
                Rotation = (component.RenderTransform as RotateTransform)?.Angle ?? 0
            });
        }

        foreach (var wire in DrawingSurface.Children.OfType<Polyline>()
                     .Where(shape => string.Equals(shape.Tag?.ToString(), "Wire", StringComparison.Ordinal)))
        {
            document.Wires.Add(new BreadboardWireFile
            {
                Thickness = wire.StrokeThickness,
                Points = wire.Points.Select(point => new BreadboardPointFile
                {
                    X = point.X,
                    Y = point.Y
                }).ToList()
            });
        }

        return document;
    }

    private void OpenDocument()
    {
        if (_activeWire is not null)
        {
            FinishActiveWire();
        }

        var dialog = new OpenFileDialog
        {
            Title = "Ouvrir un schéma",
            Filter = "Schéma MyBreadboard (*.mybrd)|*.mybrd",
            DefaultExt = ".mybrd",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        if (DrawingSurface.Children.Count > 0)
        {
            var result = MessageBox.Show(this,
                "L'ouverture remplacera le schéma actuellement affiché. Continuer ?",
                "Ouvrir un schéma",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        try
        {
            var json = File.ReadAllText(dialog.FileName);
            var document = JsonSerializer.Deserialize<BreadboardFile>(json, DocumentJsonOptions)
                ?? throw new InvalidDataException("Le fichier ne contient pas de schéma MyBreadboard valide.");

            ValidateDocument(document);
            ApplyDocument(document);
            _currentFilePath = dialog.FileName;
            UpdateWindowTitle();
            EditorStatus.Text = $"Schéma ouvert : {IOPath.GetFileName(dialog.FileName)}";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            MessageBox.Show(this,
                $"Impossible d'ouvrir ce schéma. Le contenu actuel a été conservé.\n\n{exception.Message}",
                "Erreur d'ouverture",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static void ValidateDocument(BreadboardFile document)
    {
        if (document.FormatVersion != CurrentDocumentFormatVersion)
        {
            throw new InvalidDataException($"Version de fichier non prise en charge : {document.FormatVersion}.");
        }

        if (document.Components is null || document.Wires is null ||
            document.Components.Count > MaximumDocumentItems || document.Wires.Count > MaximumDocumentItems)
        {
            throw new InvalidDataException("Le fichier contient trop d'éléments ou des listes invalides.");
        }

        foreach (var component in document.Components)
        {
            if (component is null || string.IsNullOrWhiteSpace(component.Type) ||
                !IsFinite(component.X) || !IsFinite(component.Y) ||
                !IsFinite(component.Width) || component.Width <= 0 ||
                !IsFinite(component.Height) || component.Height <= 0 ||
                !IsFinite(component.Rotation))
            {
                throw new InvalidDataException("Le fichier contient un composant avec des propriétés invalides.");
            }
        }

        foreach (var wire in document.Wires)
        {
            if (wire is null || wire.Points is null || wire.Points.Count < 2 ||
                wire.Points.Count > MaximumDocumentItems ||
                !IsFinite(wire.Thickness) || wire.Thickness <= 0)
            {
                throw new InvalidDataException("Le fichier contient un fil invalide.");
            }

            if (wire.Points.Any(point => point is null || !IsFinite(point.X) || !IsFinite(point.Y)))
            {
                throw new InvalidDataException("Le fichier contient un point de fil invalide.");
            }
        }
    }

    private void ApplyDocument(BreadboardFile document)
    {
        SelectElement(null);
        DrawingSurface.Children.Clear();

        foreach (var savedComponent in document.Components)
        {
            AddComponentToCanvas(savedComponent.Type, savedComponent.X, savedComponent.Y);
            var component = (Border)DrawingSurface.Children[^1];
            component.Width = savedComponent.Width;
            component.Height = savedComponent.Height;
            component.RenderTransformOrigin = new WpfPoint(0.5, 0.5);
            component.RenderTransform = new RotateTransform(NormalizeAngle(savedComponent.Rotation));
        }

        foreach (var savedWire in document.Wires)
        {
            var wire = new Polyline
            {
                Stroke = Brushes.Black,
                StrokeThickness = savedWire.Thickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                IsHitTestVisible = true,
                Tag = "Wire",
                ContextMenu = CreateObjectContextMenu()
            };

            foreach (var point in savedWire.Points)
            {
                wire.Points.Add(new WpfPoint(point.X, point.Y));
            }

            wire.MouseLeftButtonDown += Wire_MouseLeftButtonDown;
            wire.MouseRightButtonDown += SelectForContextMenu;
            DrawingSurface.Children.Add(wire);
        }

        SelectElement(null);
    }

    private void UpdateWindowTitle()
    {
        Title = string.IsNullOrWhiteSpace(_currentFilePath)
            ? "My Breadboard"
            : $"My Breadboard — {IOPath.GetFileName(_currentFilePath)}";
    }

    private static double NormalizeAngle(double angle)
    {
        var normalized = angle % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
