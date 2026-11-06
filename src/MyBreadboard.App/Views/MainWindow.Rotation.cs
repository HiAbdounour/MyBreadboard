using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace MyBreadboard.App.Views;

public partial class MainWindow
{
    private void Window_PreviewKeyDownWithRotation(object sender, KeyEventArgs e)
    {
        if (Keyboard.FocusedElement is TextBoxBase)
        {
            Window_PreviewKeyDown(sender, e);
            return;
        }

        if (e.Key == Key.Space)
        {
            // Rotation is only for components, never for wires or while the wire tool is active.
            if (!_isWireMode && _selectedElement is Border component &&
                !string.Equals(component.Tag?.ToString(), "Fil", StringComparison.OrdinalIgnoreCase))
            {
                var currentAngle = (component.RenderTransform as RotateTransform)?.Angle ?? 0;
                component.RenderTransformOrigin = new Point(0.5, 0.5);
                component.RenderTransform = new RotateTransform((currentAngle + 90) % 360);
                e.Handled = true;
            }

            return;
        }

        // Keep the existing W, Delete and Escape shortcuts working.
        Window_PreviewKeyDown(sender, e);
    }
}
