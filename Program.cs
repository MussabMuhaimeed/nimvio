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
        Application.Run(new NimvioApplicationContext());
    }
}
