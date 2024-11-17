using System.Runtime.InteropServices;

namespace Claudable.Utilities
{
    public static partial class ShellContextMenuHandler
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }
    }
}