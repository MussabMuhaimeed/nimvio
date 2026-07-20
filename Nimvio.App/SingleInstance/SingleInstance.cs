using System.IO.Pipes;

namespace Nimvio;

internal static class SingleInstance
{
    private const string MutexName = @"Global\MussabMuhaimeed.Nimvio.SingleInstance";
    private const string PipeName = "MussabMuhaimeed.Nimvio.Activate";

    internal static bool TryAcquire(out Mutex? mutex, string? mutexName = null, string? pipeName = null)
    {
        mutexName ??= MutexName;
        pipeName ??= PipeName;
        mutex = new Mutex(true, mutexName, out var ownsMutex);
        if (ownsMutex) 
        {
            return true;
        }

        SignalRunningInstance(pipeName);
        mutex.Dispose();
        mutex = null;
        return false;
    }

    internal static void RunActivationServer(Action onActivate, CancellationToken cancellationToken)
    {
        _ = RunActivationServerAsync(onActivate, cancellationToken, PipeName);
    }

    internal static async Task RunActivationServerAsync(
        Action onActivate,
        CancellationToken cancellationToken,
        string pipeName)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(
                    pipeName,
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
    }

    private static void SignalRunningInstance(string pipeName)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
            client.Connect(300);
            using var writer = new StreamWriter(client) { AutoFlush = true };
            writer.WriteLine("activate");
        }
        catch { }
    }
}
