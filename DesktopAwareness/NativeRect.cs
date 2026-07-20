using System.Runtime.InteropServices;

namespace Nimvio;

[StructLayout(LayoutKind.Sequential)]
internal struct NativeRect
{
    public int Left, Top, Right, Bottom;
}
