using System.Net;
using System.Net.Sockets;
using Lolcode.Runtime;

namespace Lolcode.CodeAnalysis.Tests;

/// <summary>Tests managed implementations of the pinned lci built-in libraries.</summary>
[Collection(nameof(ConsoleRuntimeCollection))]
public class LibraryRuntimeTests
{
    [Fact]
    public void Stdio_Slots_ReadWriteRewindClose_AndReportFailedOpen()
    {
        var scope = LolRuntime.CreateScope();
        LolRuntime.LoadLibrary(scope, "STDIO");
        string path = Path.Combine(AppContext.BaseDirectory, $"stdio-{Guid.NewGuid():N}.dat");
        try
        {
            object? file = Invoke(scope, "STDIO", "OPEN", path, "w+");
            Invoke(scope, "STDIO", "DIAF", file).Should().Be(false);
            Invoke(scope, "STDIO", "SCRIBBEL", file, "HAI");
            Invoke(scope, "STDIO", "AGEIN", file);
            LolRuntime.CastToYarn(Invoke(scope, "STDIO", "LUK", file, 3)).Should().Be("HAI");
            Invoke(scope, "STDIO", "CLOSE", file).Should().BeNull();
            Invoke(scope, "STDIO", "DIAF", file).Should().Be(true);

            string missing = Path.Combine(
                AppContext.BaseDirectory,
                $"missing-{Guid.NewGuid():N}",
                "file.dat");
            object? failed = Invoke(scope, "STDIO", "OPEN", missing, "r");
            Invoke(scope, "STDIO", "DIAF", failed).Should().Be(true);
        }

        finally
        {
            LolRuntime.DisposeScope(scope);
            File.Delete(path);
        }
    }

    /// <summary>Serializes tests that temporarily replace process-wide console writers.</summary>
    [CollectionDefinition(nameof(ConsoleRuntimeCollection), DisableParallelization = true)]
    public sealed class ConsoleRuntimeCollection;

    [Theory]
    [InlineData("a")]
    [InlineData("a+")]
    public void Stdio_AppendWritesAlwaysReturnToEndAfterRewind(string mode)
    {
        var scope = LolRuntime.CreateScope();
        LolRuntime.LoadLibrary(scope, "STDIO");
        string path = Path.Combine(AppContext.BaseDirectory, $"append-{Guid.NewGuid():N}.dat");
        File.WriteAllText(path, "A");
        try
        {
            object? file = Invoke(scope, "STDIO", "OPEN", path, mode);
            Invoke(scope, "STDIO", "AGEIN", file);
            Invoke(scope, "STDIO", "SCRIBBEL", file, "B").Should().BeNull();
            Invoke(scope, "STDIO", "AGEIN", file);
            Invoke(scope, "STDIO", "SCRIBBEL", file, "C").Should().BeNull();
            Invoke(scope, "STDIO", "CLOSE", file);
            File.ReadAllText(path).Should().Be("ABC");
        }
        finally
        {
            LolRuntime.DisposeScope(scope);
            File.Delete(path);
        }
    }

    [Fact]
    public void Stdio_IoAndAccessErrorsSetDiafWithoutThrowing()
    {
        var scope = LolRuntime.CreateScope();
        LolRuntime.LoadLibrary(scope, "STDIO");
        string path = Path.Combine(AppContext.BaseDirectory, $"errors-{Guid.NewGuid():N}.dat");
        File.WriteAllText(path, "HAI");
        try
        {
            object? writeOnly = Invoke(scope, "STDIO", "OPEN", path, "w");
            Invoke(scope, "STDIO", "LUK", writeOnly, 1).Should().Be("");
            Invoke(scope, "STDIO", "DIAF", writeOnly).Should().Be(true);

            object? readOnly = Invoke(scope, "STDIO", "OPEN", path, "r");
            Invoke(scope, "STDIO", "SCRIBBEL", readOnly, "NOPE").Should().BeNull();
            Invoke(scope, "STDIO", "DIAF", readOnly).Should().Be(true);

            Invoke(scope, "STDIO", "CLOSE", readOnly);
            Invoke(scope, "STDIO", "AGEIN", readOnly).Should().BeNull();
            Invoke(scope, "STDIO", "DIAF", readOnly).Should().Be(true);

            FluentActions.Invoking(() => Invoke(scope, "STDIO", "LUK", "not a BLOB", 1))
                .Should().Throw<LolRuntimeException>()
                .WithMessage("*file BLOB*");
        }
        finally
        {
            LolRuntime.DisposeScope(scope);
            File.Delete(path);
        }
    }

    [Fact]
    public void Stdlib_ReseedingIsDeterministic_AndBlowZeroIsZero()
    {
        var scope = LolRuntime.CreateScope();
        LolRuntime.LoadLibrary(scope, "STDLIB");
        try
        {
            Invoke(scope, "STDLIB", "MIX", 1234).Should().BeNull();
            object? first = Invoke(scope, "STDLIB", "BLOW", 1000);
            Invoke(scope, "STDLIB", "MIX", 1234);
            object? second = Invoke(scope, "STDLIB", "BLOW", 1000);

            second.Should().Be(first);
            Invoke(scope, "STDLIB", "BLOW", 0).Should().Be(0);
        }
        finally
        {
            LolRuntime.DisposeScope(scope);
        }
    }

    [Fact]
    public void String_SlotsUseUtf8Bytes_AndReturnEmptyOutOfBounds()
    {
        var scope = LolRuntime.CreateScope();
        LolRuntime.LoadLibrary(scope, "STRING");

        Invoke(scope, "STRING", "LEN", "é").Should().Be(2);
        object? firstByte = Invoke(scope, "STRING", "AT", "é", 0);
        object? secondByte = Invoke(scope, "STRING", "AT", "é", 1);
        LolRuntime.CastToYarn(firstByte).Should().Be("Ã");
        LolRuntime.CastToYarn(secondByte).Should().Be("©");
        Invoke(scope, "STRING", "LEN", firstByte).Should().Be(1);
        Invoke(scope, "STRING", "AT", "HAI", -1).Should().Be("");
        Invoke(scope, "STRING", "AT", "HAI", 3).Should().Be("");
    }

    [Fact]
    public void String_SelectedByteWritesWithoutUtf8Expansion()
    {
        var scope = LolRuntime.CreateScope();
        LolRuntime.LoadLibrary(scope, "STRING");
        LolRuntime.LoadLibrary(scope, "STDIO");
        string path = Path.Combine(AppContext.BaseDirectory, $"byte-yarn-{Guid.NewGuid():N}.dat");
        try
        {
            object? selected = Invoke(scope, "STRING", "AT", "é", 0);
            object? file = Invoke(scope, "STDIO", "OPEN", path, "w");
            Invoke(scope, "STDIO", "SCRIBBEL", file, selected).Should().BeNull();
            Invoke(scope, "STDIO", "CLOSE", file);

            File.ReadAllBytes(path).Should().Equal(0xC3);
        }
        finally
        {
            LolRuntime.DisposeScope(scope);
            File.Delete(path);
        }
    }

    [Fact]
    public void ByteYarns_PreserveIdentityAcrossYarnOperations()
    {
        var scope = LolRuntime.CreateScope();
        LolRuntime.LoadLibrary(scope, "STRING");

        object? first = Invoke(scope, "STRING", "AT", "é", 0);
        object? second = Invoke(scope, "STRING", "AT", "é", 1);
        object combined = LolRuntime.SmooshValue(first, second);

        Invoke(scope, "STRING", "LEN", combined).Should().Be(2);
        LolRuntime.BothSaem(combined, "é").Should().BeTrue();
        LolRuntime.Diffrint(combined, "é").Should().BeFalse();
        LolRuntime.SwitchCaseMatches(combined, "é").Should().BeTrue();
        LolRuntime.BothSaem(first, "Ã").Should().BeFalse();
        LolRuntime.SwitchCaseMatches(first, "Ã").Should().BeFalse();
        LolRuntime.BothSaem(LolRuntime.ExplicitCast(first, "YARN"), first).Should().BeTrue();

        var interpolationScope = LolRuntime.CreateScope();
        LolRuntime.DeclareValue(interpolationScope, ["I"], ["first"], first);
        LolRuntime.DeclareValue(interpolationScope, ["I"], ["second"], second);
        object interpolated = LolRuntime.InterpolateYarnValue(
            interpolationScope,
            ["", "", ""],
            ["first", "second"]);
        LolRuntime.BothSaem(interpolated, "é").Should().BeTrue();
    }

    [Fact]
    public void Print_UsesConfiguredStreamWriterForRawByteYarns()
    {
        var scope = LolRuntime.CreateScope();
        LolRuntime.LoadLibrary(scope, "STRING");
        object? first = Invoke(scope, "STRING", "AT", "é", 0);
        object? second = Invoke(scope, "STRING", "AT", "é", 1);
        TextWriter originalOutput = Console.Out;
        TextWriter originalError = Console.Error;
        using var outputBytes = new MemoryStream();
        using var errorBytes = new MemoryStream();
        using var output = new StreamWriter(
            outputBytes,
            new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            leaveOpen: true);
        using var error = new StreamWriter(
            errorBytes,
            new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            leaveOpen: true);
        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            LolRuntime.Print([first], suppressNewline: true);
            LolRuntime.Print([second], suppressNewline: true, standardError: true);
            output.Flush();
            error.Flush();
        }
        finally
        {
            Console.SetOut(originalOutput);
            Console.SetError(originalError);
        }

        outputBytes.ToArray().Should().Equal(0xC3);
        errorBytes.ToArray().Should().Equal(0xA9);
    }

    [Fact]
    public void Print_UsesConfiguredStringWriterAndPreservesValidUtf8()
    {
        var scope = LolRuntime.CreateScope();
        LolRuntime.LoadLibrary(scope, "STRING");
        object? first = Invoke(scope, "STRING", "AT", "é", 0);
        object? second = Invoke(scope, "STRING", "AT", "é", 1);
        TextWriter originalOutput = Console.Out;
        using var output = new StringWriter();
        try
        {
            Console.SetOut(output);
            LolRuntime.Print([first, second], suppressNewline: false);
        }
        finally
        {
            Console.SetOut(originalOutput);
        }

        output.ToString().Should().Be("é" + output.NewLine);
    }

    [Fact]
    public async Task Socks_SlotsResolveConnectSendReceiveClose_AndReturnEmptyAtEof()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var scope = LolRuntime.CreateScope();
        LolRuntime.LoadLibrary(scope, "SOCKS");
        LolRuntime.LoadLibrary(scope, "STRING");
        try
        {
            Invoke(scope, "SOCKS", "RESOLV", "localhost").Should().Be("127.0.0.1");
            object? local = Invoke(scope, "SOCKS", "BIND", "127.0.0.1", 0);
            Task<Socket> acceptTask = listener.AcceptSocketAsync();
            object? remote = Invoke(scope, "SOCKS", "KONN", local, "127.0.0.1", port);
            using Socket peer = await acceptTask.WaitAsync(TimeSpan.FromSeconds(5));

            Invoke(scope, "SOCKS", "PUT", local, remote, "HAI").Should().Be(3);
            var incoming = new byte[3];
            int count = await peer.ReceiveAsync(incoming).WaitAsync(TimeSpan.FromSeconds(5));
            incoming[..count].Should().Equal((byte)'H', (byte)'A', (byte)'I');

            await peer.SendAsync("KTHX"u8.ToArray()).WaitAsync(TimeSpan.FromSeconds(5));
            LolRuntime.BothSaem(
                Invoke(scope, "SOCKS", "GET", local, remote, 4),
                "KTHX").Should().BeTrue();

            await peer.SendAsync(new byte[] { 0xC3 }).WaitAsync(TimeSpan.FromSeconds(5));
            object? first = Invoke(scope, "SOCKS", "GET", local, remote, 1);
            await peer.SendAsync(new byte[] { 0xA9 }).WaitAsync(TimeSpan.FromSeconds(5));
            object? second = Invoke(scope, "SOCKS", "GET", local, remote, 1);
            object reassembled = LolRuntime.SmooshValue(first, second);
            Invoke(scope, "STRING", "LEN", reassembled).Should().Be(2);
            LolRuntime.BothSaem(reassembled, "é").Should().BeTrue();

            Invoke(scope, "SOCKS", "PUT", local, remote, reassembled).Should().Be(2);
            var echoedBytes = new byte[2];
            int echoed = await peer.ReceiveAsync(echoedBytes).WaitAsync(TimeSpan.FromSeconds(5));
            echoed.Should().Be(2);
            echoedBytes.Should().Equal(0xC3, 0xA9);
            peer.LingerState = new LingerOption(enable: true, seconds: 0);
            peer.Close();
            Invoke(scope, "SOCKS", "GET", local, remote, 4).Should().Be("");
            Invoke(scope, "SOCKS", "CLOSE", local).Should().BeSameAs(local);
        }
        finally
        {
            LolRuntime.DisposeScope(scope);
        }
    }

    [Fact]
    public void UnknownLibrary_IsIgnored()
    {
        var scope = LolRuntime.CreateScope();
        LolRuntime.LoadLibrary(scope, "BRAINZ");

        FluentActions.Invoking(() => LolRuntime.GetValue(scope, ["BRAINZ"]))
            .Should().Throw<LolRuntimeException>()
            .WithMessage("*does not exist*");
    }

    [Fact]
    public void ScopeCleanupClosesUnclosedBlobHandles()
    {
        var scope = LolRuntime.CreateScope();
        LolRuntime.LoadLibrary(scope, "STDIO");
        string path = Path.Combine(AppContext.BaseDirectory, $"cleanup-{Guid.NewGuid():N}.dat");
        object? file = Invoke(scope, "STDIO", "OPEN", path, "w");
        try
        {
            file.Should().BeAssignableTo<LolBlob>().Which.IsClosed.Should().BeFalse();
            LolRuntime.DisposeScope(scope);
            ((LolBlob)file!).IsClosed.Should().BeTrue();
        }
        finally
        {
            LolRuntime.DisposeScope(scope);
            File.Delete(path);
        }
    }

    [Fact]
    public void ExplicitCloseUnregistersBlobWhileScopeStillTracksOpenHandles()
    {
        var scope = LolRuntime.CreateScope();
        LolRuntime.LoadLibrary(scope, "STDIO");
        string firstPath = Path.Combine(AppContext.BaseDirectory, $"tracked-1-{Guid.NewGuid():N}.dat");
        string secondPath = Path.Combine(AppContext.BaseDirectory, $"tracked-2-{Guid.NewGuid():N}.dat");
        try
        {
            object? closed = Invoke(scope, "STDIO", "OPEN", firstPath, "w");
            object? open = Invoke(scope, "STDIO", "OPEN", secondPath, "w");
            GetTrackedResourceCount(scope).Should().Be(2);

            Invoke(scope, "STDIO", "CLOSE", closed);
            GetTrackedResourceCount(scope).Should().Be(1);

            LolRuntime.DisposeScope(scope);
            ((LolBlob)open!).IsClosed.Should().BeTrue();
            GetTrackedResourceCount(scope).Should().Be(0);
        }
        finally
        {
            LolRuntime.DisposeScope(scope);
            File.Delete(firstPath);
            File.Delete(secondPath);
        }
    }

    [Fact]
    public void PrintCompatibilityOverloadIsAvailable()
    {
        typeof(LolRuntime).GetMethod(
                nameof(LolRuntime.Print),
                [typeof(object[]), typeof(bool)])
            .Should().NotBeNull();
        typeof(LolRuntime).GetMethod(
                nameof(LolRuntime.Print),
                [typeof(object[]), typeof(bool), typeof(bool)])
            .Should().NotBeNull();
        typeof(LolRuntime).GetMethod(nameof(LolRuntime.Smoosh)).Should().NotBeNull();
        typeof(LolRuntime).GetMethod(nameof(LolRuntime.SmooshValue)).Should().NotBeNull();
        typeof(LolRuntime).GetMethod(nameof(LolRuntime.InterpolateYarn)).Should().NotBeNull();
        typeof(LolRuntime).GetMethod(nameof(LolRuntime.InterpolateYarnValue)).Should().NotBeNull();

        LolRuntime.Print([], suppressNewline: true);
    }

    private static object? Invoke(
        LolScope scope,
        string library,
        string function,
        params object?[] arguments) =>
        LolRuntime.Invoke(scope, [library], [function], arguments);

    private static int GetTrackedResourceCount(LolScope scope)
    {
        object tracker = typeof(LolScope)
            .GetProperty("Resources", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic)!
            .GetValue(scope)!;
        object resources = tracker.GetType()
            .GetField("_resources", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic)!
            .GetValue(tracker)!;
        return (int)resources.GetType().GetProperty("Count")!.GetValue(resources)!;
    }
}
