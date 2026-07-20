using System.Runtime.InteropServices;
using System.Text;

namespace Nimvio;

internal static class DesktopAwarenessNativeMethods
{
    [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll")] internal static extern bool IsIconic(IntPtr hwnd);

    [DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    [DllImport("user32.dll")] internal static extern bool GetGUIThreadInfo(uint idThread, ref GuiThreadInfo info);

    [DllImport("user32.dll")] internal static extern bool ClientToScreen(IntPtr hwnd, ref Point point);

    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int virtualKey);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int maximumCount);

    [DllImport("user32.dll")] internal static extern int GetWindowTextLength(IntPtr hwnd);
    
    [DllImport("user32.dll")] internal static extern bool GetLastInputInfo(ref LastInputInfo info);
}
