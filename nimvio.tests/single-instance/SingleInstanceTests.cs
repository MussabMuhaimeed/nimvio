using Nimvio;
using Xunit;

namespace Nimvio.Tests.SingleInstanceBehavior;

public sealed class SingleInstanceTests
{
    [Fact]
    public void TryAcquireWithUnusedNameOwnsMutex()
    {
        // Arrange
        var mutexName = UniqueMutexName();

        // Act
        var acquired = SingleInstance.TryAcquire(out var mutex, mutexName, UniquePipeName());

        // Assert
        Assert.True(acquired);
        Assert.NotNull(mutex);
        mutex.ReleaseMutex();
        mutex.Dispose();
    }

    [Fact]
    public void TryAcquireWhenAnotherInstanceOwnsMutexReturnsFalseAndSignalsActivation()
    {
        // Arrange
        var mutexName = UniqueMutexName();
        var pipeName = UniquePipeName();
        using var activated = new ManualResetEventSlim();
        using var cancellation = new CancellationTokenSource();
        var server = SingleInstance.RunActivationServerAsync(activated.Set, cancellation.Token, pipeName);

        // Act
        var firstAcquired = SingleInstance.TryAcquire(out var owner, mutexName, pipeName);

        // Assert
        Assert.True(firstAcquired);
        Assert.NotNull(owner);
        try
        {
            // Act
            var secondAttempt = Task.Factory.StartNew(
                () =>
                {
                    var acquired = SingleInstance.TryAcquire(out var secondMutex, mutexName, pipeName);
                    return (acquired, secondMutex);
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).GetAwaiter().GetResult();

            // Assert
            Assert.False(secondAttempt.acquired);
            Assert.Null(secondAttempt.secondMutex);
            Assert.True(activated.Wait(TimeSpan.FromSeconds(2)), "The running instance did not receive the activation signal.");
        }
        finally
        {
            owner.ReleaseMutex();
            owner.Dispose();
            cancellation.Cancel();
            Assert.True(server.Wait(TimeSpan.FromSeconds(2)), "The activation server did not stop after cancellation.");
        }
    }

    [Fact]
    public async Task RunActivationServerAsyncWhenCancelledWithoutClientStopsPromptly()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var activationCount = 0;
        var server = SingleInstance.RunActivationServerAsync(
            () => Interlocked.Increment(ref activationCount),
            cancellation.Token,
            UniquePipeName());

        // Act
        cancellation.Cancel();
        await server.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        Assert.Equal(0, activationCount);
    }

    [Fact]
    public async Task RunActivationServerAsyncAfterActivationContinuesListeningForNextSignal()
    {
        // Arrange
        var pipeName = UniquePipeName();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var activationCount = 0;
        var secondActivation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var server = SingleInstance.RunActivationServerAsync(
            () =>
            {
                if (Interlocked.Increment(ref activationCount) == 2)
                {
                    secondActivation.TrySetResult();
                }
            },
            cancellation.Token,
            pipeName);

        // Act
        await SendActivationAsync(pipeName);
        await WaitUntilAsync(() => Volatile.Read(ref activationCount) == 1);
        await SendActivationAsync(pipeName);
        await secondActivation.Task.WaitAsync(TimeSpan.FromSeconds(2));

        cancellation.Cancel();
        await server.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        Assert.Equal(2, activationCount);
    }

    private static async Task SendActivationAsync(string pipeName)
    {
        using var client = new System.IO.Pipes.NamedPipeClientStream(".", pipeName, System.IO.Pipes.PipeDirection.Out, System.IO.Pipes.PipeOptions.Asynchronous);
        await client.ConnectAsync(2_000);
        await using var writer = new StreamWriter(client) { AutoFlush = true };
        await writer.WriteLineAsync("activate");
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (!condition())
        {
            if (DateTime.UtcNow >= timeout)
            {
                throw new TimeoutException("The expected activation was not observed.");
            }
            await Task.Delay(10);
        }
    }

    private static string UniqueMutexName() => $@"Local\Nimvio.Tests.{Guid.NewGuid():N}";
    private static string UniquePipeName() => $"Nimvio.Tests.{Guid.NewGuid():N}";
}
