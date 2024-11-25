using Claudable.Utilities.Win32.Enum;
using Claudable.Utilities.Win32.Interfaces;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Claudable.Utilities;

public static partial class ShellContextMenuHandler
{
    public static void ShowContextMenu(string path, Window window, Point position)
    {
        var windowHandle = new WindowInteropHelper(window).Handle;
        IntPtr pidl = IntPtr.Zero;
        IShellFolder shellFolder = null;
        IntPtr childPidl = IntPtr.Zero;
        IntPtr hmenu = IntPtr.Zero;
        IContextMenu contextMenu = null;
        uint psfgaoOut = 0;

        try
        {
            // Initialize COM
            var hr = CoInitializeEx(IntPtr.Zero, COINIT_APARTMENTTHREADED | COINIT_DISABLE_OLE1DDE);
            if (hr < 0) Marshal.ThrowExceptionForHR(hr);

            // Get the PIDL for the file
            hr = SHParseDisplayName(path, IntPtr.Zero, out pidl, 0, out psfgaoOut);
            if (hr < 0 || pidl == IntPtr.Zero) return;

            // Get the parent folder and relative PIDL
            hr = SHBindToParent(pidl, IID_IShellFolder, out shellFolder, out childPidl);
            if (hr < 0 || shellFolder == null) return;

            // Get IContextMenu
            IntPtr[] pidlArray = new IntPtr[] { childPidl };
            var guid = IID_IContextMenu;
            hr = 0;

            try
            {
                shellFolder.GetUIObjectOf(windowHandle, 1, pidlArray, ref guid, IntPtr.Zero, out IntPtr pContextMenu);
                if (pContextMenu != IntPtr.Zero)
                {
                    contextMenu = (IContextMenu)Marshal.GetTypedObjectForIUnknown(pContextMenu, typeof(IContextMenu));
                    Marshal.Release(pContextMenu);

                    hmenu = CreatePopupMenu();
                    if (hmenu != IntPtr.Zero)
                    {
                        uint cmdFirst = 1;
                        uint cmdLast = 0x7FFF;

                        hr = contextMenu.QueryContextMenu(
                            hmenu,
                            0,
                            cmdFirst,
                            cmdLast,
                            QueryContextMenuFlags.NORMAL | QueryContextMenuFlags.EXPLORE);

                        if (hr >= 0)
                        {
                            uint command = TrackPopupMenuEx(
                                hmenu,
                                TPM_RETURNCMD | TPM_LEFTBUTTON,
                                (int)position.X,
                                (int)position.Y,
                                windowHandle,
                                IntPtr.Zero);

                            if (command > 0)
                            {
                                var invokeInfo = new CMINVOKECOMMANDINFOEX
                                {
                                    cbSize = (uint)Marshal.SizeOf(typeof(CMINVOKECOMMANDINFOEX)),
                                    lpVerb = (IntPtr)(command - cmdFirst),
                                    nShow = 1,
                                    hwnd = windowHandle,
                                    ptInvoke = new POINT { x = (int)position.X, y = (int)position.Y }
                                };

                                var invokePtr = Marshal.AllocHGlobal(Marshal.SizeOf(invokeInfo));
                                try
                                {
                                    Marshal.StructureToPtr(invokeInfo, invokePtr, false);
                                    contextMenu.InvokeCommand(invokePtr);
                                }
                                finally
                                {
                                    Marshal.FreeHGlobal(invokePtr);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in context menu: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        finally
        {
            if (hmenu != IntPtr.Zero)
                DestroyMenu(hmenu);

            if (contextMenu != null)
                Marshal.ReleaseComObject(contextMenu);

            if (shellFolder != null)
                Marshal.ReleaseComObject(shellFolder);

            if (pidl != IntPtr.Zero)
                Marshal.FreeCoTaskMem(pidl);

            CoUninitialize();
        }
    }

    [DllImport("ole32.dll")]
    private static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

    [DllImport("ole32.dll")]
    private static extern void CoUninitialize();

    [DllImport("shell32.dll")]
    private static extern int SHParseDisplayName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszName,
        IntPtr pbc,
        out IntPtr ppidl,
        uint sfgaoIn,
        out uint psfgaoOut);

    [DllImport("shell32.dll")]
    private static extern int SHBindToParent(
        IntPtr pidl,
        [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
        out IShellFolder ppv,
        out IntPtr ppidlLast);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern uint TrackPopupMenuEx(
        IntPtr hmenu,
        uint fuFlags,
        int x,
        int y,
        IntPtr hwnd,
        IntPtr lptpm);

    private static readonly Guid IID_IContextMenu = new Guid("000214E4-0000-0000-C000-000000000046");
    private static readonly Guid IID_IShellFolder = new Guid("000214E6-0000-0000-C000-000000000046");

    private const uint TPM_RETURNCMD = 0x0100;
    private const uint TPM_LEFTBUTTON = 0x0000;
    private const uint COINIT_APARTMENTTHREADED = 0x2;
    private const uint COINIT_DISABLE_OLE1DDE = 0x4;
}