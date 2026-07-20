namespace Nimvio;

internal readonly record struct WindowSnapshot(IntPtr Handle, Rectangle Bounds, string ProcessName, string Title);
