using System.IO.Pipes;

namespace Nimvio;

internal static class SingleInstance
{
    private const string MutexName = @"Global\MussabMuhaimeed.Nimvio.SingleInstance";
    private const string PipeName = "MussabMuhaimeed.Nimvio.Activate";

    internal static bool TryAcquire(out Mutex? mutex)
    {
        mutex = new Mutex(true, MutexName, out var ownsMutex);
        if (ownsMutex) 
        {
            return true;
        }

        SignalRunningInstance();
        mutex.Dispose();
        mutex = null;
        return false;
    }

    internal static void RunActivationServer(Action onActivate, CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await using var server = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(cancellationToken);
                    using var reader = new StreamReader(server);
                    _ = await reader.ReadLineAsync(cancellationToken);
                    onActivate();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    try
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }, cancellationToken);
    }

    private static void SignalRunningInstance()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(300);
            using var writer = new StreamWriter(client) { AutoFlush = true };
            writer.WriteLine("activate");
        }
        catch { }
    }
}
