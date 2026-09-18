using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Lolcode.EndToEnd.Tests;

/// <summary>Runs pinned lci fixtures that upstream does not register with CTest.</summary>
[Collection(nameof(LciConformanceCollection))]
public class LciAdditionalFixtureTests : EndToEndTestBase
{
    [Theory]
    [InlineData("4-stdlib/1-srand/test.lol")]
    [InlineData("4-stdlib/2-rand/test.lol")]
    public void UnregisteredStdlibFixtureProducesThreeBoundedValues(string relativePath)
    {
        string sourcePath = BindingFixturePath(relativePath);
        string output = CompileAndRun(File.ReadAllText(sourcePath, Encoding.UTF8));
        string[] lines = output.Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCount(3);
        lines.Select(int.Parse).Should().OnlyContain(value => value >= 0 && value < 10);

        if (relativePath.Contains("1-srand", StringComparison.Ordinal))
        {
            string reseeded = CompileAndRun(
                """
                HAI 1.4
                CAN HAS STDLIB?
                I IZ STDLIB'Z MIX YR 42 MKAY
                I HAS A first ITZ I IZ STDLIB'Z BLOW YR 1000 MKAY
                I IZ STDLIB'Z MIX YR 42 MKAY
                I HAS A second ITZ I IZ STDLIB'Z BLOW YR 1000 MKAY
                VISIBLE first
                VISIBLE second
                KTHXBYE
                """);
            string[] values = reseeded.Replace("\r\n", "\n")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries);
            values.Should().HaveCount(2);
            values[0].Should().Be(values[1]);
        }
    }

    [Fact]
    public async Task UnregisteredSocketAcceptFixtureReceivesHaiWithoutHanging()
    {
        string sourcePath = BindingFixturePath("3-socket/3-accept/test.lol");
        int port = ReserveTcpPort();
        string source = File.ReadAllText(sourcePath, Encoding.UTF8)
            .Replace("13337", port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        string assemblyPath = CompileToAssembly(source, sourcePath);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = TestDirectory,
        };
        startInfo.ArgumentList.Add(assemblyPath);

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to start socket fixture.");
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();

        try
        {
            using TcpClient client = await ConnectWithRetryAsync(process, port);
            await client.GetStream().WriteAsync("HAI"u8.ToArray());
            client.Client.Shutdown(SocketShutdown.Send);

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await process.WaitForExitAsync(timeout.Token);
            process.ExitCode.Should().Be(
                0,
                $"stderr was:{Environment.NewLine}{await error}");
            (await output).Replace("\r\n", "\n").Should().Be("CMD IZ HAI\n");
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
    }

    private static async Task<TcpClient> ConnectWithRetryAsync(Process process, int port)
    {
        DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        Exception? lastError = null;
        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
                throw new InvalidOperationException(
                    $"Socket fixture exited before accepting a connection ({process.ExitCode}).");

            var client = new TcpClient(AddressFamily.InterNetwork);
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
                await client.ConnectAsync(IPAddress.Loopback, port, timeout.Token);
                return client;
            }
            catch (Exception ex) when (
                ex is SocketException or OperationCanceledException)
            {
                lastError = ex;
                client.Dispose();
                await Task.Delay(50);
            }
        }

        throw new TimeoutException(
            $"Socket fixture did not listen on port {port}.",
            lastError);
    }

    private static int ReserveTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static string BindingFixturePath(string relativePath) =>
        Path.Combine(
            AppContext.BaseDirectory,
            "Conformance",
            "lci",
            "upstream",
            "test",
            "1.4-Tests",
            "13-Bindings",
            relativePath.Replace('/', Path.DirectorySeparatorChar));
}
