using System.Runtime.InteropServices;

namespace Nimvio;

[StructLayout(LayoutKind.Sequential)]
internal struct GuiThreadInfo
{
    public int Size, Flags;

    public IntPtr Active, Focus, Capture, MenuOwner, MoveSize, Caret;
    
    public NativeRect CaretRect;
}
