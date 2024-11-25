using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Claudable.Controls;

public class FileSystemIcon : System.Windows.Controls.Image
{
    public static readonly DependencyProperty PathProperty =
        DependencyProperty.Register("Path", typeof(string), typeof(FileSystemIcon),
            new PropertyMetadata(null, OnPathChanged));

    private static readonly Dictionary<string, ImageSource> IconCache = new Dictionary<string, ImageSource>();

    public string Path
    {
        get { return (string)GetValue(PathProperty); }
        set { SetValue(PathProperty, value); }
    }

    private static void OnPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FileSystemIcon control)
        {
            control.UpdateIcon();
        }
    }

    private void UpdateIcon()
    {
        if (string.IsNullOrEmpty(Path))
        {
            Source = null;
            return;
        }

        if (IconCache.TryGetValue(Path, out ImageSource cachedIcon))
        {
            Source = cachedIcon;
            return;
        }

        try
        {
            SHFILEINFO shfi = new SHFILEINFO();
            IntPtr hIcon = SHGetFileInfo(Path, 0, ref shfi, (uint)Marshal.SizeOf(shfi), SHGFI_ICON | SHGFI_SMALLICON);

            if (hIcon != IntPtr.Zero)
            {
                ImageSource icon = Imaging.CreateBitmapSourceFromHIcon(shfi.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                DestroyIcon(shfi.hIcon);

                IconCache[Path] = icon;
                Source = icon;
            }
        }
        catch (Exception)
        {
            // Handle or log the exception as needed
            Source = null;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32.dll")]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_SMALLICON = 0x1;
}