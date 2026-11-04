using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MyBreadboard.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AddDemoContent();
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
        DrawingSurface.Children.Add(loadRect);

        // Un tracé composé uniquement de segments horizontaux et verticaux.
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
