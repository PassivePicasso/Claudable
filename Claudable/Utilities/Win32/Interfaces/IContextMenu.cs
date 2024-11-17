using System.Runtime.InteropServices;
using Claudable.Utilities.Win32.Enum;

namespace Claudable.Utilities.Win32.Interfaces
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214E4-0000-0000-C000-000000000046")]
    public interface IContextMenu
    {
        [PreserveSig]
        int QueryContextMenu(
            nint hmenu,
            uint indexMenu,
            uint idCmdFirst,
            uint idCmdLast,
            QueryContextMenuFlags uFlags);

        [PreserveSig]
        void InvokeCommand(nint pici);

        [PreserveSig]
        void GetCommandString(
            uint idCmd,
            GetCommandStringFlags uFlags,
            nint pwReserved,
            nint pszName,
            uint cchMax);
    }
}