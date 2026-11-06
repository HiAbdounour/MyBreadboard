namespace MyBreadboard.App.Models;

/// <summary>
/// JSON document format used by .mybrd files.
/// </summary>
public sealed class BreadboardFile
{
    public int FormatVersion { get; set; } = 1;
    public List<BreadboardComponentFile> Components { get; set; } = new();
    public List<BreadboardWireFile> Wires { get; set; } = new();
}

public sealed class BreadboardComponentFile
{
    public string Type { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double Rotation { get; set; }
}

public sealed class BreadboardWireFile
{
    public List<BreadboardPointFile> Points { get; set; } = new();
    public double Thickness { get; set; }
}

public sealed class BreadboardPointFile
{
    public double X { get; set; }
    public double Y { get; set; }
}
