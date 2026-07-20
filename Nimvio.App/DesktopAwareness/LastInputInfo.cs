using System.Runtime.InteropServices;

namespace Nimvio;

[StructLayout(LayoutKind.Sequential)]
internal struct LastInputInfo
{
    public uint Size;
    
    public uint TickCount;
}
