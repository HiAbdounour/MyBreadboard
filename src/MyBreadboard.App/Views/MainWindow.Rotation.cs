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
        // Leave standard editing shortcuts untouched while the annotation editor has focus.
        if (Keyboard.FocusedElement is TextBoxBase)
        {
            Window_PreviewKeyDown(sender, e);
            return;
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            switch (e.Key)
            {
                case Key.N:
                    NewDocument_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    return;
                case Key.O:
                    OpenDocument_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    return;
                case Key.S:
                    SaveDocument_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    return;
            }
        }

        if (e.Key == Key.Space)
        {
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

        // Preserve the existing W, Delete and Escape shortcuts.
        Window_PreviewKeyDown(sender, e);
    }

    private void SetFileMenuShortcutLabels()
    {
        if (Content is not Grid root)
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
            item.InputGestureText = item.Header?.ToString() switch
            {
                "Nouveau" => "Ctrl+N",
                "Ouvrir" => "Ctrl+O",
                "Enregistrer" => "Ctrl+S",
                _ => item.InputGestureText
            };
        }
    }
}
