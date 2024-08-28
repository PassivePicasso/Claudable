using System.Windows;

namespace Claudable.Windows
{
    /// <summary>
    /// Interaction logic for DragAdorner.xaml
    /// </summary>
    public partial class DragAdorner : Window
    {
        public DragAdorner()
        {
            InitializeComponent();
        }
        public void UpdatePosition(Point cursorPosition)
        {
            this.Left = cursorPosition.X;
            this.Top = cursorPosition.Y;
        }
    }
}
