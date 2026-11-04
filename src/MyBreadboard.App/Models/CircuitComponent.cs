namespace MyBreadboard.App.Models;

public class CircuitComponent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 100;
    public double Height { get; set; } = 80;
    public string Symbol { get; set; } = "resistor";
}
