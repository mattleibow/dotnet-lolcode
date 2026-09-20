using System.Net;
using System.Net.Sockets;
using Lolcode.Runtime;
using Lolcode.Runtime.String;
using Lolcode.Runtime.Stdlib;

namespace Lolcode.CodeAnalysis.Tests;

/// <summary>Tests managed implementations of the pinned lci built-in libraries.</summary>
[Collection(nameof(ConsoleRuntimeCollection))]
public class LibraryRuntimeTests
{
    [Fact]
    public void DirectStaticImport_DoesNotCreateDuplicateModule()
    {
        var scope = CreateScope();
        int factoryCalls = 0;
        try
        {
            LolObject Factory(LolScope importingScope)
            {
                factoryCalls++;
                return LolRuntime.CreateLibraryObject(importingScope);
            }

            LolRuntime.ImportStaticLibrary(scope, "TEST", Factory);
            LolRuntime.ImportStaticLibrary(scope, "TEST", Factory);

            factoryCalls.Should().Be(1);
        }
        finally
        {
            LolRuntime.DisposeScope(scope);
        }
    }

    [Fact]
    public void StaticRegistrations_UseDirectFactoriesAndRetainPerImportState()
    {
        var first = CreateScope();
        var second = CreateScope();
        LolcodeLibraryRegistration[] registrations =
        [
            new("STRING", StringLibraryFactory.Create),
            new("STDLIB", StdlibLibraryFactory.Create),
        ];

        LolRuntime.RegisterLibraries(first, registrations);
        LolRuntime.RegisterLibraries(second, registrations);
        LolRuntime.ImportRegisteredLibrary(first, "STRING");
        LolRuntime.ImportRegisteredLibrary(first, "STDLIB");
        LolRuntime.ImportRegisteredLibrary(second, "STDLIB");

        Invoke(first, "STRING", "LEN", "é").Should().Be(2);
        Invoke(first, "STDLIB", "MIX", 1234);
        object? expected = Invoke(first, "STDLIB", "BLOW", 1000000);
        Invoke(second, "STDLIB", "MIX", 1234);
        Invoke(second, "STDLIB", "BLOW", 1000000).Should().Be(expected);
        FluentActions.Invoking(() => LolRuntime.ImportRegisteredLibrary(first, "UNKNOWN"))
            .Should().Throw<LolRuntimeException>()
            .WithMessage("*not declared*");

        LolRuntime.DisposeScope(first);
        LolRuntime.DisposeScope(second);
    }

    [Fact]
    public void Stdio_Slots_ReadWriteRewindClose_AndReportFailedOpen()
    {
        var scope = CreateScope();
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
        var scope = CreateScope();
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
        var scope = CreateScope();
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
            FluentActions.Invoking(() => Invoke(scope, "STDIO", "AGEIN", readOnly))
                .Should().Throw<LolRuntimeException>()
                .WithMessage("*closed BLOB*");
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
    public void Stdio_CoercionFailuresSurfaceWithoutSettingDiaf_WhileIoFailuresSetIt()
    {
        var scope = CreateScope();
        LolRuntime.LoadLibrary(scope, "STDIO");
        string path = Path.Combine(AppContext.BaseDirectory, $"coercion-{Guid.NewGuid():N}.dat");
        try
        {
            object? writable = Invoke(scope, "STDIO", "OPEN", path, "w");
            object[] invalidValues =
            [
                new LolObject(scope),
                new LolFunction(0, static (_, _, _, _) => null, []),
                new TestBlob(),
            ];

            foreach (object value in invalidValues)
            {
                FluentActions.Invoking(() => Invoke(scope, "STDIO", "SCRIBBEL", writable, value))
                    .Should().Throw<LolRuntimeException>()
                    .WithMessage("*YARN*");
                Invoke(scope, "STDIO", "DIAF", writable).Should().Be(false);
            }

            object? readOnly = Invoke(scope, "STDIO", "OPEN", path, "r");
            Invoke(scope, "STDIO", "SCRIBBEL", readOnly, "NOPE").Should().BeNull();
            Invoke(scope, "STDIO", "DIAF", readOnly).Should().Be(true);

            Invoke(scope, "STDIO", "CLOSE", writable);
            FluentActions.Invoking(() => Invoke(scope, "STDIO", "LUK", writable, 1))
                .Should().Throw<LolRuntimeException>()
                .WithMessage("*closed BLOB*");
            FluentActions.Invoking(() => Invoke(scope, "STDIO", "AGEIN", writable))
                .Should().Throw<LolRuntimeException>()
                .WithMessage("*closed BLOB*");
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
        var scope = CreateScope();
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
        var scope = CreateScope();
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
        var scope = CreateScope();
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
        var scope = CreateScope();
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
        var scope = CreateScope();
        LolRuntime.LoadLibrary(scope, "STRING");
        object? first = Invoke(scope, "STRING", "AT", "é", 0);
        object? second = Invoke(scope, "STRING", "AT", "é", 1);
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
        using (LolRuntime.PushIo(
            new StringReader(string.Empty),
            output,
            error,
            outputBytes,
            errorBytes))
        {
            LolRuntime.Print([first], suppressNewline: true);
            LolRuntime.Print([second], suppressNewline: true, standardError: true);
            output.Flush();
            error.Flush();
        }

        outputBytes.ToArray().Should().Equal(0xC3);
        errorBytes.ToArray().Should().Equal(0xA9);
    }

    [Fact]
    public void Print_UsesConfiguredStringWriterAndPreservesValidUtf8()
    {
        var scope = CreateScope();
        LolRuntime.LoadLibrary(scope, "STRING");
        object? first = Invoke(scope, "STRING", "AT", "é", 0);
        object? second = Invoke(scope, "STRING", "AT", "é", 1);
        using var output = new StringWriter();
        using (LolRuntime.PushIo(new StringReader(string.Empty), output, TextWriter.Null))
        {
            LolRuntime.Print([first, second], suppressNewline: false);
        }

        output.ToString().Should().Be("é" + output.NewLine);
    }

    [Fact]
    public void Print_UsesScopedStandardError()
    {
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        using var ioScope = LolRuntime.PushIo(
            new StringReader(string.Empty),
            standardOutput,
            standardError);

        LolRuntime.Print(["error"], suppressNewline: true, standardError: true);

        standardOutput.ToString().Should().BeEmpty();
        standardError.ToString().Should().Be("error");
    }

    [Fact]
    public void ExecuteSystemCommand_UsesScopedStandardError()
    {
        string command = OperatingSystem.IsWindows()
            ? "echo scoped-error 1>&2"
            : "printf scoped-error >&2";
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        using var ioScope = LolRuntime.PushIo(
            new StringReader(string.Empty),
            standardOutput,
            standardError);

        LolRuntime.ExecuteSystemCommandValue(command);

        standardOutput.ToString().Should().BeEmpty();
        standardError.ToString().Should().Contain("scoped-error");
    }

    [Fact]
    public async Task Socks_SlotsResolveConnectSendReceiveClose_AndReturnEmptyAtEof()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var scope = CreateScope();
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
    public async Task Socks_TransferredAliasSurvivesOriginalCloseAndScopeDisposal()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var originalScope = CreateScope();
        var aliasScope = CreateScope();
        LolRuntime.LoadLibrary(originalScope, "SOCKS");
        aliasScope.Values["SOCKS"] = LolRuntime.GetValue(originalScope, ["SOCKS"]);
        try
        {
            object? original = Invoke(originalScope, "SOCKS", "BIND", "127.0.0.1", 0);
            Task<Socket> acceptTask = listener.AcceptSocketAsync();
            object? alias = Invoke(aliasScope, "SOCKS", "KONN", original, "127.0.0.1", port);
            using Socket peer = await acceptTask.WaitAsync(TimeSpan.FromSeconds(5));

            GetTrackedResourceCount(originalScope).Should().Be(1);
            GetTrackedResourceCount(aliasScope).Should().Be(1);
            Invoke(originalScope, "SOCKS", "CLOSE", original).Should().BeSameAs(original);
            GetTrackedResourceCount(originalScope).Should().Be(0);
            LolRuntime.DisposeScope(originalScope);

            Invoke(aliasScope, "SOCKS", "PUT", alias, alias, "HAI").Should().Be(3);
            var bytes = new byte[3];
            int received = await peer.ReceiveAsync(bytes).WaitAsync(TimeSpan.FromSeconds(5));
            bytes[..received].Should().Equal((byte)'H', (byte)'A', (byte)'I');

            Invoke(aliasScope, "SOCKS", "CLOSE", alias).Should().BeSameAs(alias);
            GetTrackedResourceCount(aliasScope).Should().Be(0);
            FluentActions.Invoking(() => Invoke(aliasScope, "SOCKS", "PUT", alias, alias, "KTHX"))
                .Should().Throw<LolRuntimeException>()
                .WithMessage("*closed BLOB*");
        }
        finally
        {
            LolRuntime.DisposeScope(originalScope);
            LolRuntime.DisposeScope(aliasScope);
        }
    }

    [Fact]
    public void UnknownLibrary_IsIgnored()
    {
        var scope = CreateScope();
        LolRuntime.LoadLibrary(scope, "BRAINZ");

        FluentActions.Invoking(() => LolRuntime.GetValue(scope, ["BRAINZ"]))
            .Should().Throw<LolRuntimeException>()
            .WithMessage("*does not exist*");
    }

    [Fact]
    public void ScopeCleanupClosesUnclosedBlobHandles()
    {
        var scope = CreateScope();
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
        var scope = CreateScope();
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
    public void ClosedBlobsAreNeverTrackedIncludingRepeatedSocketClose()
    {
        var scope = CreateScope();
        var context = new LolcodeLibraryContext(scope.Resources);
        var closed = new TestBlob();
        closed.Dispose();
        context.RegisterResource(closed).Should().BeSameAs(closed);
        GetTrackedResourceCount(scope).Should().Be(0);

        LolRuntime.LoadLibrary(scope, "SOCKS");
        object? socket = Invoke(scope, "SOCKS", "BIND", "127.0.0.1", 0);
        GetTrackedResourceCount(scope).Should().Be(1);
        Invoke(scope, "SOCKS", "CLOSE", socket);
        Invoke(scope, "SOCKS", "CLOSE", socket);
        GetTrackedResourceCount(scope).Should().Be(0);
        LolRuntime.DisposeScope(scope);
    }

    [Fact]
    public void ClosedResultsFromRegisteredProvidersAreNotAdopted()
    {
        var scope = LolRuntime.CreateScope();
        string assemblyName = typeof(ClosedBlobProvider).Assembly.GetName().Name!;
        string typeName = typeof(ClosedBlobProvider).FullName!;
        scope.Libraries.Register(
            "CLOSED",
            assemblyName,
            typeName,
            isBuiltIn: false,
            1);
        LolRuntime.LoadLibrary(scope, "CLOSED");

        object? result = Invoke(scope, "CLOSED", "CLOSED");

        result.Should().BeOfType<ClosedProviderBlob>().Which.IsClosed.Should().BeTrue();
        GetTrackedResourceCount(scope).Should().Be(0);
    }

    [Fact]
    public void RegisteredLibraryResourcesBelongToTheirInvocationScope()
    {
        var importingScope = CreateScope();
        var callingScope = CreateScope();
        string path = Path.Combine(AppContext.BaseDirectory, $"escaped-stdio-{Guid.NewGuid():N}.dat");
        LolRuntime.LoadLibrary(importingScope, "STDIO");
        callingScope.Values["STDIO"] = LolRuntime.GetValue(importingScope, ["STDIO"]);
        try
        {
            object? file = Invoke(callingScope, "STDIO", "OPEN", path, "w");
            GetTrackedResourceCount(importingScope).Should().Be(0);
            GetTrackedResourceCount(callingScope).Should().Be(1);

            LolRuntime.DisposeScope(importingScope);
            ((LolBlob)file!).IsClosed.Should().BeFalse();
            LolRuntime.DisposeScope(callingScope);
            ((LolBlob)file).IsClosed.Should().BeTrue();
        }
        finally
        {
            LolRuntime.DisposeScope(importingScope);
            LolRuntime.DisposeScope(callingScope);
            File.Delete(path);
        }
    }

    [Fact]
    public void LibraryModuleInvocationRetainsModuleStateAndUsesCallerOwnership()
    {
        var importingScope = LolRuntime.CreateScope();
        var callingScope = LolRuntime.CreateScope();
        string assemblyName = typeof(InvocationScopeProvider).Assembly.GetName().Name!;
        string typeName = typeof(InvocationScopeProvider).FullName!;
        importingScope.Libraries.Register(
            "SCOPE",
            assemblyName,
            typeName,
            isBuiltIn: true,
            1);
        var module = LolRuntime.CreateLibraryObject(importingScope);
        module.Values["moduleState"] = "from module";
        LolRuntime.LoadLibrary(module, "SCOPE");
        module.Values["OPEN"] = new LolFunction(
            0,
            (caller, receiver, _, _) =>
            {
                LolScope invocation = LolRuntime.CreateInvocationScope(caller, receiver);
                invocation.Parent.Should().BeSameAs(module);
                invocation.Caller.Should().BeSameAs(module);
                invocation.Resources.Should().BeSameAs(callingScope.Resources);
                invocation.Libraries.Should().BeSameAs(callingScope.Libraries);
                LolRuntime.GetValue(invocation, ["moduleState"]).Should().Be("from module");
                LolRuntime.GetValue(invocation, ["ME", "moduleState"]).Should().Be("from module");
                return Invoke(invocation, "SCOPE", "OPEN");
            },
            []);
        module.Values["COUNT"] = new LolFunction(
            0,
            (caller, receiver, _, _) =>
            {
                LolScope invocation = LolRuntime.CreateInvocationScope(caller, receiver);
                return Invoke(invocation, "SCOPE", "COUNT");
            },
            []);
        callingScope.Values["MODULE"] = module;

        try
        {
            object? first = Invoke(callingScope, "MODULE", "OPEN");

            GetTrackedResourceCount(importingScope).Should().Be(0);
            GetTrackedResourceCount(callingScope).Should().Be(1);
            ((LolBlob)first!).IsClosed.Should().BeFalse();
            LolRuntime.DisposeScope(importingScope);
            ((LolBlob)first).IsClosed.Should().BeFalse();

            Invoke(callingScope, "MODULE", "COUNT").Should().Be(1);
            object? second = Invoke(callingScope, "MODULE", "OPEN");
            Invoke(callingScope, "MODULE", "COUNT").Should().Be(2);
            GetTrackedResourceCount(callingScope).Should().Be(2);

            LolRuntime.DisposeScope(callingScope);
            ((LolBlob)first).IsClosed.Should().BeTrue();
            ((LolBlob)second!).IsClosed.Should().BeTrue();
        }
        finally
        {
            LolRuntime.DisposeScope(importingScope);
            LolRuntime.DisposeScope(callingScope);
        }
    }

    [Fact]
    public void EscapedRegisteredLibraryAllocatesForCallerAfterImporterDisposal()
    {
        var importingScope = CreateScope();
        LolRuntime.LoadLibrary(importingScope, "STDIO");
        object? stdio = LolRuntime.GetValue(importingScope, ["STDIO"]);
        LolRuntime.DisposeScope(importingScope);

        var callingScope = CreateScope();
        string path = Path.Combine(AppContext.BaseDirectory, $"post-disposal-stdio-{Guid.NewGuid():N}.dat");
        callingScope.Values["STDIO"] = stdio;
        try
        {
            object? file = Invoke(callingScope, "STDIO", "OPEN", path, "w");

            ((LolBlob)file!).IsClosed.Should().BeFalse();
            GetTrackedResourceCount(callingScope).Should().Be(1);
        }
        finally
        {
            LolRuntime.DisposeScope(callingScope);
            File.Delete(path);
        }
    }

    [Fact]
    public void StdlibRandomStateIsSharedPerImportAndIsolatedAcrossImports()
    {
        var firstImport = CreateScope();
        var caller = CreateScope();
        var secondImport = CreateScope();
        LolRuntime.LoadLibrary(firstImport, "STDLIB");
        LolRuntime.LoadLibrary(secondImport, "STDLIB");
        caller.Values["STDLIB"] = LolRuntime.GetValue(firstImport, ["STDLIB"]);
        try
        {
            Invoke(firstImport, "STDLIB", "MIX", 1234);
            int firstValue = (int)Invoke(caller, "STDLIB", "BLOW", 1000000)!;
            int nextValue = (int)Invoke(firstImport, "STDLIB", "BLOW", 1000000)!;

            Invoke(secondImport, "STDLIB", "MIX", 1234);
            Invoke(secondImport, "STDLIB", "BLOW", 1000000).Should().Be(firstValue);
            Invoke(secondImport, "STDLIB", "BLOW", 1000000).Should().Be(nextValue);
        }
        finally
        {
            LolRuntime.DisposeScope(firstImport);
            LolRuntime.DisposeScope(caller);
            LolRuntime.DisposeScope(secondImport);
        }
    }

    [Fact]
    public void PublicWrapperTransfer_DetachesNestedCyclicBlobGraph()
    {
        var scope = LolRuntime.CreateScope();
        var context = new LolcodeLibraryContext(scope.Resources);
        var blob = context.RegisterResource(new TestBlob());
        var root = new LolObject(scope);
        var nested = new LolObject(root);
        scope.Values["prototypeBlob"] = blob;
        root.Values["direct"] = blob;
        root.Values["nested"] = nested;
        nested.Values["duplicate"] = blob;
        nested.Values["cycle"] = root;

        LolRuntime.TransferPublicLibraryResult(scope, nested);
        LolRuntime.DisposeScope(scope);

        blob.IsClosed.Should().BeFalse("the managed caller owns all reachable returned BLOBs");
        blob.Dispose();
        blob.IsClosed.Should().BeTrue();
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

    private static LolScope CreateScope()
    {
        var scope = LolRuntime.CreateScope();
        scope.Libraries.Register(
            "STRING",
            "Lolcode.Runtime.String",
            "Lolcode.Runtime.String.StringLibrary",
            isBuiltIn: true,
            1);
        scope.Libraries.Register(
            "STDLIB",
            "Lolcode.Runtime.Stdlib",
            "Lolcode.Runtime.Stdlib.StdlibLibrary",
            isBuiltIn: true,
            1);
        scope.Libraries.Register(
            "STDIO",
            "Lolcode.Runtime.Stdio",
            "Lolcode.Runtime.Stdio.StdioLibrary",
            isBuiltIn: true,
            1);
        scope.Libraries.Register(
            "SOCKS",
            "Lolcode.Runtime.Socks",
            "Lolcode.Runtime.Socks.SocksLibrary",
            isBuiltIn: true,
            1);
        return scope;
    }

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

    private sealed class TestBlob : LolBlob
    {
        protected override void DisposeCore()
        {
        }
    }

    public static class ClosedBlobProvider
    {
        public static object CLOSED()
        {
            var blob = new ClosedProviderBlob();
            blob.Dispose();
            return blob;
        }
    }

    public static class InvocationScopeProvider
    {
        public static object OPEN(LolcodeLibraryContext context)
        {
            var state = context.GetOrCreateState(static () => new InvocationScopeProviderState());
            state.Count++;
            return new InvocationScopeProviderBlob();
        }

        public static int COUNT(LolcodeLibraryContext context) =>
            context.GetOrCreateState(static () => new InvocationScopeProviderState()).Count;
    }

    private sealed class InvocationScopeProviderState
    {
        public int Count { get; set; }
    }

    private sealed class InvocationScopeProviderBlob : LolBlob
    {
        protected override void DisposeCore()
        {
        }
    }

    public sealed class ClosedProviderBlob : LolBlob
    {
        protected override void DisposeCore()
        {
        }
    }
}
