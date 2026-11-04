namespace MyBreadboard.App.Models;

public class Annotation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Text { get; set; } = "Annotation";
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 160;
    public double Height { get; set; } = 60;
}
