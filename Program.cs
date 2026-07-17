namespace Nimvio;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
#if DEBUG
        if (args.Length == 2 && args[0] == "--render-about")
        {
            AboutForm.SavePreview(args[1]);
            return;
        }
#endif
        if (!SingleInstance.TryAcquire(out var mutex))
        {
            return;
        }

        try
        {
            Application.Run(new NimvioApplicationContext());
        }
        finally
        {
            mutex?.ReleaseMutex();
            mutex?.Dispose();
        }
    }
}
