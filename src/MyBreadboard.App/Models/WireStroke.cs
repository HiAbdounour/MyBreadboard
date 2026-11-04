namespace MyBreadboard.App.Models;

public class WireStroke
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public List<Point> Points { get; set; } = new();
    public double Thickness { get; set; } = 2;
}

public class Point
{
    public double X { get; set; }
    public double Y { get; set; }
}
