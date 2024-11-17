namespace Claudable.Utilities.Win32.Enum
{
    [Flags]
    public enum QueryContextMenuFlags : uint
    {
        NORMAL = 0x00000000,
        DEFAULTONLY = 0x00000001,
        VERBS = 0x00000002,
        EXPLORE = 0x00000004,
        CANRENAME = 0x00000010,
        CANDELETE = 0x00000020,
        CASCADED = 0x00000040,
        NODEFAULT = 0x00000020,
        NO_ITEMS = 0x00008000
    }
}