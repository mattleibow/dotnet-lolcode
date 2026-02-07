using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using Lolcode.CodeAnalysis;
using Lolcode.CodeAnalysis.Syntax;
using Xunit.Abstractions;

namespace Lolcode.CodeAnalysis.Tests;

/// <summary>
/// Tests that verify PDB debugging symbols work with a real debugger (vsdbg).
/// Requires VS Code C# extension to be installed (provides vsdbg).
/// These tests communicate with vsdbg via DAP (Debug Adapter Protocol) over stdin/stdout.
///
/// DISABLED: vsdbg 18.x fails with error 0x89720009 on macOS (known code-signing issue).
/// See: https://github.com/dotnet/vscode-csharp/issues/7785
/// TODO: Re-enable when vsdbg supports .NET 10 on macOS or when a workaround is found.
/// PDB correctness is verified by PortablePdbTests and DebuggerSmokeTests instead.
/// </summary>
public sealed class VsdbgDebuggerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _runtimeDll;
    private readonly string? _vsdbgPath;
    private readonly ITestOutputHelper _output;

    public VsdbgDebuggerTests(ITestOutputHelper output)
    {
        _output = output;
        _tempDir = Path.Combine(Path.GetTempPath(), "lolcode-vsdbg-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _runtimeDll = Path.Combine(AppContext.BaseDirectory, "Lolcode.Runtime.dll");
        _vsdbgPath = FindVsdbg();
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    private static string? FindVsdbg()
    {
        // Allow override via environment variable
        var envPath = Environment.GetEnvironmentVariable("VSDBG_PATH");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
            return envPath;

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var searchDirs = new[]
        {
            Path.Combine(home, ".vscode", "extensions"),
            Path.Combine(home, ".vscode-insiders", "extensions"),
            Path.Combine(home, ".vscode-server", "extensions"),
        };

        var isWindows = OperatingSystem.IsWindows();
        var binaryName = isWindows ? "vsdbg.exe" : "vsdbg";

        foreach (var searchDir in searchDirs)
        {
            if (!Directory.Exists(searchDir)) continue;

            try
            {
                // Search for vsdbg in C# extension directories
                var candidates = Directory.GetFiles(searchDir, binaryName, SearchOption.AllDirectories)
                    .Where(p => p.Contains("ms-dotnettools.csharp") && p.Contains(".debugger"))
                    .OrderDescending()
                    .ToList();

                if (candidates.Count > 0)
                    return candidates[0];
            }
            catch { /* ignore search errors */ }
        }

        return null;
    }

    private (string DllPath, string PdbPath, string SourcePath) CompileToTempDir(string source, string lolFileName)
    {
        var sourcePath = Path.Combine(_tempDir, lolFileName);
        File.WriteAllText(sourcePath, source);

        var tree = SyntaxTree.ParseText(source, sourcePath);
        var compilation = LolcodeCompilation.Create(tree);
        var outputPath = Path.Combine(_tempDir, Path.GetFileNameWithoutExtension(lolFileName) + ".dll");
        var result = compilation.Emit(outputPath, _runtimeDll);

        result.Success.Should().BeTrue(string.Join("\n", result.Diagnostics.Select(d => d.ToString())));

        // Copy runtime DLL next to output
        var runtimeDest = Path.Combine(_tempDir, "Lolcode.Runtime.dll");
        if (!File.Exists(runtimeDest))
            File.Copy(_runtimeDll, runtimeDest, overwrite: true);

        var dllPath = result.OutputPath!;
        var pdbPath = Path.ChangeExtension(dllPath, ".pdb");
        return (dllPath, pdbPath, sourcePath);
    }

    // TODO: Re-enable when vsdbg works on macOS with .NET 10.
    // vsdbg 18.x fails with error 0x89720009 on configurationDone — this affects ALL .NET apps
    // (including standard C# console apps), not just LOLCODE. The same error occurs on both
    // .NET 9 and .NET 10, with both vsdbg and vsdbg-ui, even after re-signing with ad-hoc
    // entitlements. The C# Dev Kit uses a different managed debugger (VSDebugCore) which is
    // not usable via command-line DAP.
    //
    // [Fact]
    // public async Task Vsdbg_BreakpointHit_SourceAndLineCorrect()
    // {
    //     if (_vsdbgPath == null)
    //     {
    //         _output.WriteLine("SKIP: vsdbg not found. Install VS Code C# extension or set VSDBG_PATH.");
    //         return;
    //     }
    //
    //     var source = """
    //         HAI 1.2
    //           I HAS A x ITZ 42
    //           I HAS A y ITZ 7
    //           VISIBLE x
    //           VISIBLE y
    //         KTHXBYE
    //         """;
    //
    //     var (dllPath, pdbPath, sourcePath) = CompileToTempDir(source, "vsdbg_bp.lol");
    //     File.Exists(pdbPath).Should().BeTrue("PDB must exist for debugger test");
    //
    //     // Log PDB contents for diagnostics
    //     using (var pdbStream = File.OpenRead(pdbPath))
    //     using (var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream))
    //     {
    //         var reader = provider.GetMetadataReader();
    //         foreach (var docHandle in reader.Documents)
    //         {
    //             var doc = reader.GetDocument(docHandle);
    //             _output.WriteLine($"PDB Document: '{reader.GetString(doc.Name)}'");
    //         }
    //         foreach (var handle in reader.MethodDebugInformation)
    //         {
    //             var info = reader.GetMethodDebugInformation(handle);
    //             foreach (var sp in info.GetSequencePoints())
    //                 if (!sp.IsHidden)
    //                     _output.WriteLine($"  SP: L{sp.StartLine}:C{sp.StartColumn} -> L{sp.EndLine}:C{sp.EndColumn}");
    //         }
    //     }
    //
    //     using var client = new DapClient(_vsdbgPath);
    //     await client.StartAsync();
    //
    //     // Initialize
    //     var initResp = await client.RequestAsync("initialize", new
    //     {
    //         clientID = "lolcode-test",
    //         adapterID = "coreclr",
    //         pathFormat = "path",
    //         linesStartAt1 = true,
    //         columnsStartAt1 = true,
    //     });
    //     if (!initResp.GetProperty("success").GetBoolean())
    //     {
    //         _output.WriteLine($"SKIP: vsdbg initialize failed: {initResp}");
    //         return;
    //     }
    //
    //     // Launch with stopAtEntry
    //     var launchResp = await client.RequestAsync("launch", new
    //     {
    //         type = "coreclr",
    //         request = "launch",
    //         program = dllPath,
    //         args = Array.Empty<string>(),
    //         cwd = Path.GetDirectoryName(dllPath)!,
    //         justMyCode = false,
    //         stopAtEntry = true,
    //     }, timeoutMs: 30_000);
    //     if (!launchResp.GetProperty("success").GetBoolean())
    //     {
    //         _output.WriteLine($"SKIP: vsdbg launch failed: {launchResp}");
    //         return;
    //     }
    //
    //     // Configuration done
    //     var confResp = await client.RequestAsync("configurationDone", new { });
    //     if (!confResp.GetProperty("success").GetBoolean())
    //     {
    //         _output.WriteLine($"SKIP: vsdbg configurationDone failed (vsdbg {GetVsdbgVersion()} may not support .NET 10): {confResp}");
    //         return;
    //     }
    //
    //     // Wait for entry stop
    //     var entryStop = await client.WaitForEventAsync("stopped", TimeSpan.FromSeconds(30));
    //     entryStop.Should().NotBeNull("should stop at entry point");
    //     var threadId = entryStop!.Value.GetProperty("body").GetProperty("threadId").GetInt32();
    //
    //     // Set breakpoint on VISIBLE x (line 4)
    //     var bpResp = await client.RequestAsync("setBreakpoints", new
    //     {
    //         source = new { path = sourcePath },
    //         breakpoints = new[] { new { line = 4 } },
    //         sourceModified = false,
    //     });
    //     bpResp.GetProperty("success").GetBoolean().Should().BeTrue(
    //         $"setBreakpoints should succeed. Response: {bpResp}");
    //
    //     // Continue to breakpoint
    //     await client.RequestAsync("continue", new { threadId });
    //
    //     var stoppedEvent = await client.WaitForEventAsync("stopped", TimeSpan.FromSeconds(30));
    //     stoppedEvent.Should().NotBeNull("should hit breakpoint");
    //     threadId = stoppedEvent!.Value.GetProperty("body").GetProperty("threadId").GetInt32();
    //
    //     // Verify stack frame
    //     var stackResp = await client.RequestAsync("stackTrace", new
    //     {
    //         threadId, startFrame = 0, levels = 5,
    //     });
    //     stackResp.GetProperty("success").GetBoolean().Should().BeTrue();
    //     var topFrame = stackResp.GetProperty("body").GetProperty("stackFrames")[0];
    //     topFrame.GetProperty("source").GetProperty("path").GetString()
    //         .Should().EndWith("vsdbg_bp.lol");
    //     topFrame.GetProperty("line").GetInt32().Should().Be(4);
    //
    //     // Verify locals
    //     var scopesResp = await client.RequestAsync("scopes", new
    //     {
    //         frameId = topFrame.GetProperty("id").GetInt32(),
    //     });
    //     var localsRef = scopesResp.GetProperty("body").GetProperty("scopes")[0]
    //         .GetProperty("variablesReference").GetInt32();
    //     var varsResp = await client.RequestAsync("variables", new { variablesReference = localsRef });
    //     var varNames = new List<string>();
    //     foreach (var v in varsResp.GetProperty("body").GetProperty("variables").EnumerateArray())
    //         varNames.Add(v.GetProperty("name").GetString()!);
    //
    //     varNames.Should().Contain("x", "local variable 'x' should be visible in debugger");
    //     _output.WriteLine("PASS: Debugger breakpoint hit, source/line correct, locals visible");
    //
    //     // Clean up
    //     await client.RequestAsync("continue", new { threadId });
    //     await client.RequestAsync("disconnect", new { restart = false, terminateDebuggee = true });
    // }

    private string GetVsdbgVersion()
    {
        try
        {
            // Extract version from vsdbg path (e.g., ms-dotnettools.csharp-2.120.3)
            if (_vsdbgPath != null)
            {
                var match = System.Text.RegularExpressions.Regex.Match(_vsdbgPath, @"csharp-(\d+\.\d+\.\d+)");
                if (match.Success) return match.Groups[1].Value;
            }
        }
        catch { }
        return "unknown";
    }

    /// <summary>
    /// Minimal DAP client that communicates with vsdbg over stdin/stdout.
    /// Kept for future use when vsdbg macOS issues are resolved.
    /// </summary>
    private sealed class DapClient : IDisposable
    {
        private readonly string _vsdbgPath;
        private Process? _process;
        private int _seq;
        private readonly Dictionary<int, TaskCompletionSource<JsonElement>> _pending = new();
        private readonly List<JsonElement> _events = new();
        private readonly SemaphoreSlim _eventSignal = new(0, int.MaxValue);
        private CancellationTokenSource? _cts;
        private Task? _readerTask;

        public DapClient(string vsdbgPath) => _vsdbgPath = vsdbgPath;

        public Task StartAsync()
        {
            _cts = new CancellationTokenSource();
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _vsdbgPath,
                    Arguments = "--interpreter=vscode",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
            };
            _process.Start();
            _readerTask = Task.Run(() => ReaderLoopAsync(_cts.Token));
            return Task.CompletedTask;
        }

        public async Task<JsonElement> RequestAsync(string command, object? arguments, int timeoutMs = 15_000)
        {
            var seq = Interlocked.Increment(ref _seq);
            var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);

            lock (_pending)
                _pending[seq] = tcs;

            var msg = new { seq, type = "request", command, arguments };
            await SendMessageAsync(msg);

            using var cts = new CancellationTokenSource(timeoutMs);
            cts.Token.Register(() => tcs.TrySetCanceled());

            return await tcs.Task;
        }

        public async Task<JsonElement?> WaitForEventAsync(string eventName, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                lock (_events)
                {
                    for (int i = 0; i < _events.Count; i++)
                    {
                        if (_events[i].TryGetProperty("event", out var e) &&
                            e.GetString() == eventName)
                        {
                            var result = _events[i];
                            _events.RemoveAt(i);
                            return result;
                        }
                    }
                }

                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero) break;
                try
                {
                    await _eventSignal.WaitAsync(
                        remaining > TimeSpan.FromMilliseconds(500) ? TimeSpan.FromMilliseconds(500) : remaining);
                }
                catch (OperationCanceledException) { }
            }
            return null;
        }

        private async Task SendMessageAsync<T>(T msg)
        {
            var json = JsonSerializer.SerializeToUtf8Bytes(msg);
            var header = Encoding.ASCII.GetBytes($"Content-Length: {json.Length}\r\n\r\n");
            var stream = _process!.StandardInput.BaseStream;
            await stream.WriteAsync(header);
            await stream.WriteAsync(json);
            await stream.FlushAsync();
        }

        private async Task ReaderLoopAsync(CancellationToken ct)
        {
            var stdout = _process!.StandardOutput.BaseStream;
            var headerBytes = new List<byte>(256);

            while (!ct.IsCancellationRequested)
            {
                headerBytes.Clear();
                // Read headers until \r\n\r\n
                while (true)
                {
                    var buf = new byte[1];
                    int read = await stdout.ReadAsync(buf, 0, 1, ct);
                    if (read == 0) return; // EOF
                    headerBytes.Add(buf[0]);
                    int n = headerBytes.Count;
                    if (n >= 4 &&
                        headerBytes[n - 4] == '\r' && headerBytes[n - 3] == '\n' &&
                        headerBytes[n - 2] == '\r' && headerBytes[n - 1] == '\n')
                        break;
                }

                var header = Encoding.ASCII.GetString(headerBytes.ToArray());
                var length = 0;
                foreach (var line in header.Split("\r\n", StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                    {
                        length = int.Parse(line.Split(':', 2)[1].Trim());
                        break;
                    }
                }

                if (length <= 0) continue;

                var payload = new byte[length];
                int totalRead = 0;
                while (totalRead < length)
                {
                    int r = await stdout.ReadAsync(payload, totalRead, length - totalRead, ct);
                    if (r == 0) return;
                    totalRead += r;
                }

                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement.Clone();

                var type = root.GetProperty("type").GetString();
                if (type == "response")
                {
                    var requestSeq = root.GetProperty("request_seq").GetInt32();
                    TaskCompletionSource<JsonElement>? tcs;
                    lock (_pending)
                        _pending.Remove(requestSeq, out tcs);
                    tcs?.TrySetResult(root);
                }
                else if (type == "event")
                {
                    lock (_events)
                        _events.Add(root);
                    try { _eventSignal.Release(); } catch { }
                }
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            try
            {
                if (_process != null && !_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
            catch { }
            _process?.Dispose();
            _cts?.Dispose();
            _eventSignal.Dispose();
        }
    }
}
