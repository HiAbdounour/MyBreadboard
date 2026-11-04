using System.Collections.ObjectModel;

namespace MyBreadboard.App.Models;

public class DrawingDocument
{
    public const double Width = 3000;
    public const double Height = 2000;

    public ObservableCollection<CircuitComponent> Components { get; } = new();
    public ObservableCollection<WireStroke> Wires { get; } = new();
    public ObservableCollection<Annotation> Annotations { get; } = new();

    public string FileName { get; set; } = "untitled.mybreadboard";
    public bool IsModified { get; set; }
}
