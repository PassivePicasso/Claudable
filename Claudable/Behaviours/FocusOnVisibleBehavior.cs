using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace Claudable.Behaviors;

public class FocusOnVisibleBehavior : Behavior<TextBox>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.IsVisibleChanged += OnVisibleChanged;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.IsVisibleChanged -= OnVisibleChanged;
        base.OnDetaching();
    }

    private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
        {
            AssociatedObject.Focus();
            AssociatedObject.SelectAll();
        }
    }
}