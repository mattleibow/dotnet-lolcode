using Lolcode.CodeAnalysis.Scripting;
using Lolcode.CodeAnalysis.Syntax;
using Lolcode.Runtime;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;

namespace Lolcode.CodeAnalysis.Tests;

public sealed class InMemoryExecutionTests
{
    private static string HelloProgram => FixtureSource("in-memory-hello", "test.lol");

    private static string FixtureSource(params string[] path) =>
        File.ReadAllText(Path.Combine([AppContext.BaseDirectory, "Compatibility", "DotNet", "1.2", "CodeAnalysis", .. path]));

    private static string CompatibilityFixtureSource(params string[] path) =>
        File.ReadAllText(Path.Combine([AppContext.BaseDirectory, "Compatibility", .. path]));

    [Fact]
    public void Emit_ToStreams_CreatesNoFiles()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var sourcePath = Path.Combine(tempDirectory, "memory.lol");
            var compilation = LolcodeCompilation.Create(SyntaxTree.ParseText(HelloProgram, sourcePath));
            using var peStream = new MemoryStream();
            using var pdbStream = new MemoryStream();

            var result = compilation.Emit(peStream, pdbStream);

            result.Success.Should().BeTrue();
            result.OutputPath.Should().BeNull();
            result.PdbPath.Should().BeNull();
            peStream.Length.Should().BeGreaterThan(0);
            pdbStream.Length.Should().BeGreaterThan(0);
            Directory.EnumerateFileSystemEntries(tempDirectory).Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Emit_ToStreams_PropagatesPdbWriteFailures()
    {
        var compilation = LolcodeCompilation.Create(
            SyntaxTree.ParseText(HelloProgram, "program.lol"));
        using var peStream = new MemoryStream();
        using var pdbStream = new ThrowingWriteStream();

        var emit = () => compilation.Emit(peStream, pdbStream);

        emit.Should().Throw<IOException>()
            .WithMessage("Injected PDB serialization failure.");
    }

    [Fact]
    public void Emit_ToStreams_WorksWhenCompilerAndRuntimeAreLoadedFromBytes()
    {
        byte[] compilerImage = File.ReadAllBytes(typeof(LolcodeCompilation).Assembly.Location);
        byte[] runtimeImage = File.ReadAllBytes(typeof(LolRuntime).Assembly.Location);
        var loadContext = new AssemblyLoadContext(
            $"InMemoryCompiler_{Guid.NewGuid():N}",
            isCollectible: true);
        Assembly runtimeAssembly;

        try
        {
            using (var runtimeStream = new MemoryStream(runtimeImage, writable: false))
                runtimeAssembly = loadContext.LoadFromStream(runtimeStream);
            loadContext.Resolving += (_, assemblyName) =>
                AssemblyName.ReferenceMatchesDefinition(assemblyName, runtimeAssembly.GetName())
                    ? runtimeAssembly
                    : null;

            Assembly compilerAssembly;
            using (var compilerStream = new MemoryStream(compilerImage, writable: false))
                compilerAssembly = loadContext.LoadFromStream(compilerStream);

            compilerAssembly.Location.Should().BeEmpty();
            runtimeAssembly.Location.Should().BeEmpty();

            Type syntaxTreeType = compilerAssembly.GetType(
                "Lolcode.CodeAnalysis.Syntax.SyntaxTree",
                throwOnError: true)!;
            object syntaxTree = syntaxTreeType.GetMethod(
                "ParseText",
                [typeof(string), typeof(string)])!
                .Invoke(null, [HelloProgram, "memory.lol"])!;
            Array trees = Array.CreateInstance(syntaxTreeType, 1);
            trees.SetValue(syntaxTree, 0);

            Type compilationType = compilerAssembly.GetType(
                "Lolcode.CodeAnalysis.LolcodeCompilation",
                throwOnError: true)!;
            object compilation = compilationType.GetMethod("Create")!
                .Invoke(null, [trees])!;
            using var peStream = new MemoryStream();
            using var pdbStream = new MemoryStream();
            object result = compilationType.GetMethod(
                "Emit",
                [typeof(Stream), typeof(Stream), typeof(CancellationToken)])!
                .Invoke(compilation, [peStream, pdbStream, CancellationToken.None])!;

            result.GetType().GetProperty("Success")!.GetValue(result).Should().Be(true);
            peStream.Length.Should().BeGreaterThan(0);
            pdbStream.Length.Should().BeGreaterThan(0);
            ((System.Collections.IEnumerable)result.GetType()
                    .GetProperty("Diagnostics")!
                    .GetValue(result)!)
                .Cast<object>()
                .Should()
                .BeEmpty();
        }
        finally
        {
            loadContext.Unload();
        }
    }

    [Fact]
    public void Run_CreatesNoFiles()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var script = LolcodeScript.Create(HelloProgram, new LolcodeScriptOptions
            {
                FilePath = Path.Combine(tempDirectory, "submission.lol"),
            });

            var state = script.Run();

            state.Success.Should().BeTrue();
            state.Script.Should().BeSameAs(script);
            Directory.EnumerateFileSystemEntries(tempDirectory).Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Run_CapturesVisibleOutput()
    {
        var script = LolcodeScript.Create(HelloProgram);

        var state = script.Run();

        state.Script.Should().BeSameAs(script);
        state.Success.Should().BeTrue();
        state.Executed.Should().BeTrue();
        state.Diagnostics.Should().NotContain(d => d.Severity == DiagnosticSeverity.Error);
        state.StandardOutput.Should().Be($"HAI FROM MEMORY{Environment.NewLine}");
        state.StandardError.Should().BeEmpty();
        state.StandardOutputBytes.Should().Equal(System.Text.Encoding.UTF8.GetBytes($"HAI FROM MEMORY{Environment.NewLine}"));
        state.StandardOutputTruncated.Should().BeFalse();
        state.StandardErrorTruncated.Should().BeFalse();
        state.ReturnValue.Should().BeNull();
        state.Exception.Should().BeNull();
    }

    [Fact]
    public void Create_ExposesReusableCompilation()
    {
        var script = LolcodeScript.Create(HelloProgram);

        var compilation = script.GetCompilation();

        script.GetCompilation().Should().BeSameAs(compilation);
        script.Compile().Should().BeEquivalentTo(compilation.GetDiagnostics());
        script.Options.Should().BeSameAs(LolcodeScriptOptions.Default);
    }

    [Theory]
    [InlineData(6, "ABCDE", false)]
    [InlineData(5, "ABCDE", false)]
    [InlineData(4, "ABCD", true)]
    public void Run_BoundsCapturedStandardOutputBytes(
        int maximumStandardOutputBytes,
        string expectedOutput,
        bool expectedTruncated)
    {
        var script = LolcodeScript.Create(
            """
            HAI 1.2
              VISIBLE "ABCDE"!
            KTHXBYE
            """);

        var state = script.Run(new LolcodeScriptExecutionOptions
        {
            MaximumStandardOutputBytes = maximumStandardOutputBytes,
        });

        state.Success.Should().BeTrue();
        state.StandardOutput.Should().Be(expectedOutput);
        state.StandardOutputTruncated.Should().Be(expectedTruncated);
        state.StandardErrorTruncated.Should().BeFalse();
    }

    [Fact]
    public void Run_BoundsStandardStreamsIndependentlyAndUsesReplacementForPartialUtf8()
    {
        var state = LolcodeScript.Run(
            """
            HAI 1.2
              VISIBLE "é"!
              INVISIBLE "é"!
            KTHXBYE
            """,
            executionOptions: new LolcodeScriptExecutionOptions
            {
                MaximumStandardOutputBytes = 1,
                MaximumStandardErrorBytes = 2,
            });

        state.Success.Should().BeTrue();
        state.StandardOutputBytes.Should().Equal(0xC3);
        state.StandardOutput.Should().Be("\uFFFD");
        state.StandardOutputTruncated.Should().BeTrue();
        state.StandardErrorBytes.Should().Equal(0xC3, 0xA9);
        state.StandardError.Should().Be("é");
        state.StandardErrorTruncated.Should().BeFalse();
    }

    [Fact]
    public void Run_RejectsNegativeMaximumStandardStreamByteLimits()
    {
        var createOutputOptions = () => new LolcodeScriptExecutionOptions
        {
            MaximumStandardOutputBytes = -1,
        };
        var createErrorOptions = () => new LolcodeScriptExecutionOptions
        {
            MaximumStandardErrorBytes = -1,
        };

        createOutputOptions.Should().Throw<ArgumentOutOfRangeException>()
            .Which.ParamName.Should().Be("MaximumStandardOutputBytes");
        createErrorOptions.Should().Throw<ArgumentOutOfRangeException>()
            .Which.ParamName.Should().Be("MaximumStandardErrorBytes");
    }

    [Fact]
    public void Run_CapturesStandardOutputAndStandardErrorIndependently()
    {
        var state = LolcodeScript.Run(
            """
            HAI 1.2
              VISIBLE "stdout"!
              INVISIBLE "stderr"!
            KTHXBYE
            """);

        state.Success.Should().BeTrue();
        state.StandardOutput.Should().Be("stdout");
        state.StandardError.Should().Be("stderr");
    }

    [Fact]
    public void Run_PreservesRawYarnBytesOnBothStandardStreams()
    {
        var state = LolcodeScript.Run(CompatibilityFixtureSource(
            "DotNet/1.4/FutureFeature/visible-and-invisible-write-selected-raw-bytes-to-process-streams/test.lol"));

        state.Success.Should().BeTrue();
        state.StandardOutputBytes.Should().Equal(0xC3);
        state.StandardErrorBytes.Should().Equal(0xA9);
        state.StandardOutput.Should().Be("\uFFFD");
        state.StandardError.Should().Be("\uFFFD");
    }

    [Fact]
    public void Run_RoutesSourceBomToScopedStandardOutput()
    {
        var state = LolcodeScript.Run(
            "\uFEFFHAI 1.2\nVISIBLE \"output\"!\nKTHXBYE");

        state.Success.Should().BeTrue();
        state.StandardOutputBytes.Should().Equal(0xEF, 0xBB, 0xBF, (byte)'o', (byte)'u', (byte)'t', (byte)'p', (byte)'u', (byte)'t');
        state.StandardErrorBytes.Should().BeEmpty();
    }

    [Fact]
    public void Run_SuppliesInputToGimmeh()
    {
        var state = LolcodeScript.Run(
            """
            HAI 1.2
              I HAS A name
              GIMMEH name
              VISIBLE "HAI, " name "!"
            KTHXBYE
            """,
            executionOptions: new LolcodeScriptExecutionOptions
            {
                StandardInput = $"LOLCAT{Environment.NewLine}",
            });

        state.Success.Should().BeTrue();
        state.StandardOutput.Should().Be($"HAI, LOLCAT!{Environment.NewLine}");
    }

    [Fact]
    public void Run_CompilationDiagnosticsPreventExecution()
    {
        var script = LolcodeScript.Create(
            """
            HAI 1.2
              VISIBLE missing
            KTHXBYE
            """);

        var diagnostics = script.Compile();
        var state = script.Run();

        diagnostics.Should().Contain(d => d.Severity == DiagnosticSeverity.Error);
        state.Script.Should().BeSameAs(script);
        state.Success.Should().BeFalse();
        state.Executed.Should().BeFalse();
        state.Diagnostics.Should().BeEquivalentTo(diagnostics);
        state.StandardOutput.Should().BeEmpty();
        state.StandardError.Should().BeEmpty();
        state.Exception.Should().BeNull();
    }

    [Fact]
    public void Run_UnwrapsRuntimeExceptions()
    {
        var state = LolcodeScript.Run(
            """
            HAI 1.2
              I HAS A value
              VISIBLE SUM OF value AN 1
            KTHXBYE
            """);

        state.Success.Should().BeFalse();
        state.Executed.Should().BeTrue();
        state.Exception.Should().BeOfType<LolRuntimeException>()
            .Which.Message.Should().Contain("NOOB");
        state.Exception.Should().NotBeOfType<System.Reflection.TargetInvocationException>();
    }

    [Fact]
    public void Run_ExecutesFunctionsAndControlFlow()
    {
        var state = LolcodeScript.Run(
            """
            HAI 1.2
              HOW IZ I factorial YR n
                BOTH SAEM n AN 0
                O RLY?
                  YA RLY
                    FOUND YR 1
                OIC
                FOUND YR PRODUKT OF n AN I IZ factorial YR DIFF OF n AN 1 MKAY
              IF U SAY SO

              VISIBLE I IZ factorial YR 5 MKAY
            KTHXBYE
            """);

        state.Success.Should().BeTrue();
        state.StandardOutput.Should().Be($"120{Environment.NewLine}");
    }

    [Fact]
    public void Run_RepeatedExecutionsSucceed()
    {
        var script = LolcodeScript.Create(HelloProgram);

        var states = Enumerable.Range(0, 20)
            .Select(_ => script.Run())
            .ToArray();

        states.Should().OnlyContain(state => state.Success && ReferenceEquals(state.Script, script));
        states.Select(state => state.StandardOutput)
            .Should().OnlyContain(output => output == $"HAI FROM MEMORY{Environment.NewLine}");
    }

    [Fact]
    public void Run_NonCollectibleLoaderSupportsRepeatedExecution()
    {
        var script = LolcodeScript.Create(HelloProgram);

        var states = Enumerable.Range(0, 3)
            .Select(_ => script.RunCore(
                options: null,
                useNonCollectibleAssemblyLoad: true))
            .ToArray();

        states.Should().OnlyContain(state => state.Success);
        script.NonCollectibleLoadCount.Should().Be(1);
        states.Select(state => state.StandardOutput)
            .Should().OnlyContain(output => output == $"HAI FROM MEMORY{Environment.NewLine}");
    }

    [Fact]
    public void Run_NonCollectibleLoaderCachesAssembliesPerScript()
    {
        var firstScript = LolcodeScript.Create(HelloProgram);
        var secondScript = LolcodeScript.Create(HelloProgram);

        firstScript.RunCore(options: null, useNonCollectibleAssemblyLoad: true).Success.Should().BeTrue();
        firstScript.RunCore(options: null, useNonCollectibleAssemblyLoad: true).Success.Should().BeTrue();
        secondScript.RunCore(options: null, useNonCollectibleAssemblyLoad: true).Success.Should().BeTrue();

        firstScript.NonCollectibleLoadCount.Should().Be(1);
        secondScript.NonCollectibleLoadCount.Should().Be(1);
    }

    [Fact]
    public void Run_EmitsPdbOnlyWhenEnabled()
    {
        var withoutDebugInformation = LolcodeScript.Create(HelloProgram);
        var withDebugInformation = LolcodeScript.Create(HelloProgram, new LolcodeScriptOptions
        {
            EmitDebugInformation = true,
        });

        withoutDebugInformation.Compile().Should().NotContain(d => d.Severity == DiagnosticSeverity.Error);
        withDebugInformation.Compile().Should().NotContain(d => d.Severity == DiagnosticSeverity.Error);

        withoutDebugInformation.HasCachedPdb.Should().BeFalse();
        withDebugInformation.HasCachedPdb.Should().BeTrue();
        withoutDebugInformation.NonCollectibleLoadCount.Should().Be(0);
        withDebugInformation.NonCollectibleLoadCount.Should().Be(0);

        withoutDebugInformation.RunCore(options: null, useNonCollectibleAssemblyLoad: true).Success.Should().BeTrue();
        withDebugInformation.RunCore(options: null, useNonCollectibleAssemblyLoad: true).Success.Should().BeTrue();

        withoutDebugInformation.NonCollectibleLoadCount.Should().Be(1);
        withDebugInformation.NonCollectibleLoadCount.Should().Be(1);
    }

    [Fact]
    public void Emit_ToStreams_UsesStableCompilationIdentity()
    {
        var firstCompilation = LolcodeCompilation.Create(SyntaxTree.ParseText(HelloProgram, "first.lol"));
        var secondCompilation = LolcodeCompilation.Create(SyntaxTree.ParseText(HelloProgram, "second.lol"));
        using var firstStream = new MemoryStream();
        using var repeatedStream = new MemoryStream();
        using var secondStream = new MemoryStream();

        firstCompilation.Emit(firstStream).Success.Should().BeTrue();
        firstCompilation.Emit(repeatedStream).Success.Should().BeTrue();
        secondCompilation.Emit(secondStream).Success.Should().BeTrue();

        GetAssemblyName(firstStream.ToArray()).Should().Be(GetAssemblyName(repeatedStream.ToArray()));
        GetAssemblyName(firstStream.ToArray()).Should().NotBe(GetAssemblyName(secondStream.ToArray()));
    }

    [Fact]
    public void CompileEmitAndRun_RespectPreCanceledTokens()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var script = LolcodeScript.Create(HelloProgram);
        var compilation = script.GetCompilation();
        using var peStream = new MemoryStream();

        Action compile = () => script.Compile(cancellationSource.Token);
        Action diagnostics = () => compilation.GetDiagnostics(cancellationSource.Token);
        Action emit = () => compilation.Emit(peStream, cancellationToken: cancellationSource.Token);
        Action run = () => script.RunCore(
            options: null,
            useNonCollectibleAssemblyLoad: true,
            cancellationToken: cancellationSource.Token);
        Action staticRun = () => LolcodeScript.Run(HelloProgram, cancellationToken: cancellationSource.Token);

        compile.Should().Throw<OperationCanceledException>();
        diagnostics.Should().Throw<OperationCanceledException>();
        emit.Should().Throw<OperationCanceledException>();
        run.Should().Throw<OperationCanceledException>();
        staticRun.Should().Throw<OperationCanceledException>();
        peStream.Length.Should().Be(0);
        script.NonCollectibleLoadCount.Should().Be(0);
    }

    [Fact]
    public async Task Run_ParallelExecutionsKeepInputAndStandardStreamsScoped()
    {
        const string program = """
            HAI 1.2
              I HAS A value
              GIMMEH value
              VISIBLE value
              INVISIBLE value
            KTHXBYE
            """;

        var script = LolcodeScript.Create(program);
        var executions = Enumerable.Range(0, 12)
            .Select(index => Task.Run(() => script.Run(new LolcodeScriptExecutionOptions
            {
                StandardInput = $"LOLCAT {index}{Environment.NewLine}",
            })))
            .ToArray();

        var states = await Task.WhenAll(executions);

        states.Should().OnlyContain(state => state.Success);
        states.Select(state => state.StandardOutput)
            .Should().BeEquivalentTo(
                Enumerable.Range(0, 12).Select(index => $"LOLCAT {index}{Environment.NewLine}"));
        states.Select(state => state.StandardError)
            .Should().BeEquivalentTo(
                Enumerable.Range(0, 12).Select(index => $"LOLCAT {index}{Environment.NewLine}"));
    }

    [Fact]
    public void Emit_ToPath_PreservesDllPdbAndRuntimeConfigBehavior()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var sourcePath = Path.Combine(tempDirectory, "program.lol");
            var outputPath = Path.Combine(tempDirectory, "program.dll");
            var compilation = LolcodeCompilation.Create(SyntaxTree.ParseText(HelloProgram, sourcePath));

            var result = compilation.Emit(outputPath, typeof(LolRuntime).Assembly.Location);

            result.Success.Should().BeTrue();
            result.OutputPath.Should().Be(outputPath);
            result.PdbPath.Should().Be(Path.ChangeExtension(outputPath, ".pdb"));
            File.Exists(outputPath).Should().BeTrue();
            File.Exists(Path.ChangeExtension(outputPath, ".pdb")).Should().BeTrue();
            File.Exists(Path.ChangeExtension(outputPath, ".runtimeconfig.json")).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Emit_ToPath_DoesNotDeployAdjacentProviderAssemblies()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var outputPath = Path.Combine(tempDirectory, "program.dll");
            var compilation = LolcodeCompilation.Create(SyntaxTree.ParseText(HelloProgram));

            var result = compilation.Emit(outputPath, typeof(LolRuntime).Assembly.Location);

            result.Success.Should().BeTrue();
            Directory.EnumerateFiles(tempDirectory, "Lolcode.Runtime.*.dll")
                .Should()
                .BeEmpty("API consumers deploy resolved provider assets explicitly");
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Theory]
    [InlineData(".runtimeconfig.json")]
    [InlineData(".dll")]
    public void Emit_ToPath_RestoresPreviousArtifactsWhenRequiredReplacementFails(
        string failingExtension)
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var outputPath = Path.Combine(tempDirectory, "program.dll");
            var pdbPath = Path.ChangeExtension(outputPath, ".pdb");
            var runtimeConfigPath = Path.ChangeExtension(outputPath, ".runtimeconfig.json");
            var oldDll = new byte[] { 1, 2, 3 };
            var oldPdb = new byte[] { 4, 5, 6 };
            var oldRuntimeConfig = new byte[] { 7, 8, 9 };
            File.WriteAllBytes(outputPath, oldDll);
            File.WriteAllBytes(pdbPath, oldPdb);
            File.WriteAllBytes(runtimeConfigPath, oldRuntimeConfig);

            var failingPath = Path.ChangeExtension(outputPath, failingExtension);
            var fileSystem = new FaultingPathEmitFileSystem
            {
                MoveFailure = (sourcePath, destinationPath) =>
                    destinationPath == failingPath && IsStagedPath(sourcePath)
            };
            var compilation = CreatePathCompilation(tempDirectory);

            var result = compilation.Emit(
                outputPath,
                typeof(LolRuntime).Assembly.Location,
                fileSystem);

            result.Success.Should().BeFalse();
            File.ReadAllBytes(outputPath).Should().Equal(oldDll);
            File.ReadAllBytes(pdbPath).Should().Equal(oldPdb);
            File.ReadAllBytes(runtimeConfigPath).Should().Equal(oldRuntimeConfig);
            AssertNoTransactionFiles(tempDirectory);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Emit_ToPath_OmitsPdbWhenPdbReplacementFails()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var outputPath = Path.Combine(tempDirectory, "program.dll");
            var pdbPath = Path.ChangeExtension(outputPath, ".pdb");
            var runtimeConfigPath = Path.ChangeExtension(outputPath, ".runtimeconfig.json");
            File.WriteAllText(outputPath, "old dll");
            File.WriteAllText(pdbPath, "old pdb");
            File.WriteAllText(runtimeConfigPath, "old runtime config");

            var fileSystem = new FaultingPathEmitFileSystem
            {
                MoveFailure = (sourcePath, destinationPath) =>
                    destinationPath == pdbPath && IsStagedPath(sourcePath)
            };
            var compilation = CreatePathCompilation(tempDirectory);

            var result = compilation.Emit(
                outputPath,
                typeof(LolRuntime).Assembly.Location,
                fileSystem);

            result.Success.Should().BeTrue();
            result.PdbPath.Should().BeNull();
            File.Exists(pdbPath).Should().BeFalse();
            AssertPathAssemblyHasNoPdbReference(outputPath);
            AssertPathAssemblyRuns(outputPath);
            AssertNoTransactionFiles(tempDirectory);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Emit_ToPath_OmitsPdbWhenExistingPdbCannotBeBackedUpOrRemoved()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var outputPath = Path.Combine(tempDirectory, "program.dll");
            var pdbPath = Path.ChangeExtension(outputPath, ".pdb");
            var runtimeConfigPath = Path.ChangeExtension(outputPath, ".runtimeconfig.json");
            const string oldPdb = "locked old pdb";
            File.WriteAllText(outputPath, "old dll");
            File.WriteAllText(pdbPath, oldPdb);
            File.WriteAllText(runtimeConfigPath, "old runtime config");

            var fileSystem = new FaultingPathEmitFileSystem
            {
                MoveFailure = (sourcePath, destinationPath) =>
                    sourcePath == pdbPath
                    && destinationPath.EndsWith(".bak", StringComparison.Ordinal),
                DeleteFailure = path => path == pdbPath
            };
            var compilation = CreatePathCompilation(tempDirectory);

            var result = compilation.Emit(
                outputPath,
                typeof(LolRuntime).Assembly.Location,
                fileSystem);

            result.Success.Should().BeTrue();
            result.PdbPath.Should().BeNull();
            result.Diagnostics.Should().ContainSingle(diagnostic =>
                diagnostic.Id == "LOL9002"
                && diagnostic.Message.Contains(pdbPath, StringComparison.Ordinal));
            File.ReadAllText(pdbPath).Should().Be(oldPdb);
            AssertPathAssemblyHasNoPdbReference(outputPath);
            AssertPathAssemblyRuns(outputPath);
            AssertNoTransactionFiles(tempDirectory);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Emit_ToPath_OmitsPdbWhenPdbStagingFails()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var outputPath = Path.Combine(tempDirectory, "program.dll");
            var pdbPath = Path.ChangeExtension(outputPath, ".pdb");
            File.WriteAllText(pdbPath, "stale pdb");
            var fileSystem = new FaultingPathEmitFileSystem
            {
                CreateFailure = path =>
                    path.StartsWith($"{pdbPath}.", StringComparison.Ordinal)
                    && IsStagedPath(path)
            };
            var compilation = CreatePathCompilation(tempDirectory);

            var result = compilation.Emit(
                outputPath,
                typeof(LolRuntime).Assembly.Location,
                fileSystem);

            result.Success.Should().BeTrue();
            result.PdbPath.Should().BeNull();
            File.Exists(pdbPath).Should().BeFalse();
            AssertPathAssemblyHasNoPdbReference(outputPath);
            AssertPathAssemblyRuns(outputPath);
            AssertNoTransactionFiles(tempDirectory);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Emit_ToPath_ReportsPdbStagingArtifactWhenInitialAndFinalCleanupFail()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var outputPath = Path.Combine(tempDirectory, "program.dll");
            var pdbPath = Path.ChangeExtension(outputPath, ".pdb");
            bool IsPdbTemporaryPath(string path) =>
                path.StartsWith($"{pdbPath}.", StringComparison.Ordinal)
                && IsStagedPath(path);
            var cleanupAttempts = 0;
            var fileSystem = new FaultingPathEmitFileSystem
            {
                CreateFileOverride = path => IsPdbTemporaryPath(path)
                    ? new PartialWriteFailureStream(path)
                    : null,
                DeleteFailure = path =>
                {
                    if (!IsPdbTemporaryPath(path))
                        return false;

                    cleanupAttempts++;
                    return true;
                }
            };
            var compilation = CreatePathCompilation(tempDirectory);

            var result = compilation.Emit(
                outputPath,
                typeof(LolRuntime).Assembly.Location,
                fileSystem);

            result.Success.Should().BeTrue();
            result.PdbPath.Should().BeNull();
            AssertPathAssemblyHasNoPdbReference(outputPath);
            AssertPathAssemblyRuns(outputPath);

            var warning = result.Diagnostics.Should().ContainSingle(diagnostic =>
                diagnostic.Id == "LOL9002").Which;
            warning.Message.Should().Contain("obsolete output artifact");
            warning.Message.Should().Contain("Injected partial PDB staging failure.");
            warning.Message.Should().Contain("Injected delete failure");
            var transactionFiles = Directory.EnumerateFiles(tempDirectory)
                .Where(path =>
                    path.EndsWith(".tmp", StringComparison.Ordinal)
                    || path.EndsWith(".bak", StringComparison.Ordinal))
                .ToArray();
            var leakedTemporaryPath = transactionFiles.Should().ContainSingle().Which;
            leakedTemporaryPath.Should().EndWith(".tmp");
            new FileInfo(leakedTemporaryPath).Length.Should().BeGreaterThan(0);
            warning.Message.Should().Contain(leakedTemporaryPath);
            cleanupAttempts.Should().Be(2);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Emit_ToPath_OmitsPdbWhenPdbSerializationFails()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var outputPath = Path.Combine(tempDirectory, "program.dll");
            var pdbPath = Path.ChangeExtension(outputPath, ".pdb");
            var compilation = CreatePathCompilation(tempDirectory);

            var result = compilation.Emit(
                outputPath,
                typeof(LolRuntime).Assembly.Location,
                PhysicalPathEmitFileSystem.Instance,
                () => new ThrowingWriteStream());

            result.Success.Should().BeTrue();
            result.PdbPath.Should().BeNull();
            File.Exists(pdbPath).Should().BeFalse();
            AssertPathAssemblyHasNoPdbReference(outputPath);
            AssertPathAssemblyRuns(outputPath);
            AssertNoTransactionFiles(tempDirectory);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Emit_ToPath_RemovesStalePdbWhenSymbolsAreNotRequested()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var outputPath = Path.Combine(tempDirectory, "program.dll");
            var pdbPath = Path.ChangeExtension(outputPath, ".pdb");
            File.WriteAllText(pdbPath, "stale pdb");
            var compilation = LolcodeCompilation.Create(SyntaxTree.ParseText(HelloProgram));

            var result = compilation.Emit(outputPath, typeof(LolRuntime).Assembly.Location);

            result.Success.Should().BeTrue();
            result.PdbPath.Should().BeNull();
            File.Exists(pdbPath).Should().BeFalse();
            AssertPathAssemblyHasNoPdbReference(outputPath);
            AssertPathAssemblyRuns(outputPath);
            AssertNoTransactionFiles(tempDirectory);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Emit_ToPath_ReportsRuntimeLoadFailuresAsDiagnostics()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var outputPath = Path.Combine(tempDirectory, "program.dll");
            var compilation = LolcodeCompilation.Create(SyntaxTree.ParseText(HelloProgram));

            var result = compilation.Emit(outputPath, Path.Combine(tempDirectory, "missing-runtime.dll"));

            result.Success.Should().BeFalse();
            result.Diagnostics.Should().Contain(d => d.Id == "LOL9001");
            File.Exists(outputPath).Should().BeFalse();
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static LolcodeCompilation CreatePathCompilation(string tempDirectory)
    {
        var sourcePath = Path.Combine(tempDirectory, "program.lol");
        return LolcodeCompilation.Create(SyntaxTree.ParseText(HelloProgram, sourcePath));
    }

    private static void AssertPathAssemblyRuns(string outputPath)
    {
        var loadContext = new AssemblyLoadContext(
            $"LolcodePathTest_{Guid.NewGuid():N}",
            isCollectible: true);
        loadContext.Resolving += (_, assemblyName) =>
            AssemblyName.ReferenceMatchesDefinition(
                assemblyName,
                typeof(LolRuntime).Assembly.GetName())
                ? typeof(LolRuntime).Assembly
                : null;

        try
        {
            using var peStream = File.OpenRead(outputPath);
            var assembly = loadContext.LoadFromStream(peStream);
            using var output = new StringWriter();
            using var ioScope = LolRuntime.PushIo(new StringReader(string.Empty), output, TextWriter.Null);

            assembly.EntryPoint!.Invoke(obj: null, parameters: null);

            output.ToString().Should().Be($"HAI FROM MEMORY{Environment.NewLine}");
        }
        finally
        {
            loadContext.Unload();
        }
    }

    private static void AssertPathAssemblyHasNoPdbReference(string outputPath)
    {
        using var stream = File.OpenRead(outputPath);
        using var peReader = new PEReader(stream);

        peReader.ReadDebugDirectory().Should().BeEmpty();
    }

    private static bool IsStagedPath(string path)
        => path.EndsWith(".tmp", StringComparison.Ordinal);

    private static void AssertNoTransactionFiles(string tempDirectory)
    {
        Directory.EnumerateFiles(tempDirectory)
            .Should().NotContain(path =>
                path.EndsWith(".tmp", StringComparison.Ordinal)
                || path.EndsWith(".bak", StringComparison.Ordinal));
    }

    private static string GetAssemblyName(byte[] peBytes)
    {
        using var stream = new MemoryStream(peBytes, writable: false);
        using var peReader = new PEReader(stream);
        var metadataReader = peReader.GetMetadataReader();
        return metadataReader.GetString(metadataReader.GetAssemblyDefinition().Name);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "lolcode-in-memory-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class FaultingPathEmitFileSystem : IPathEmitFileSystem
    {
        public Predicate<string>? CreateFailure { get; init; }

        public Func<string, Stream?>? CreateFileOverride { get; init; }

        public Func<string, string, bool>? MoveFailure { get; init; }

        public Predicate<string>? DeleteFailure { get; init; }

        public bool FileExists(string path) => File.Exists(path);

        public void CreateDirectory(string path) => Directory.CreateDirectory(path);

        public Stream CreateNewFile(string path)
        {
            if (CreateFailure?.Invoke(path) == true)
                throw new IOException($"Injected create failure for '{path}'.");

            var overrideStream = CreateFileOverride?.Invoke(path);
            if (overrideStream != null)
                return overrideStream;

            return new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        }

        public void MoveFile(string sourcePath, string destinationPath, bool overwrite)
        {
            if (MoveFailure?.Invoke(sourcePath, destinationPath) == true)
                throw new IOException($"Injected move failure for '{destinationPath}'.");

            File.Move(sourcePath, destinationPath, overwrite);
        }

        public void DeleteFile(string path)
        {
            if (DeleteFailure?.Invoke(path) == true)
                throw new IOException($"Injected delete failure for '{path}'.");

            File.Delete(path);
        }
    }

    private sealed class ThrowingWriteStream : MemoryStream
    {
        public override void Write(byte[] buffer, int offset, int count)
            => throw new IOException("Injected PDB serialization failure.");

        public override void Write(ReadOnlySpan<byte> buffer)
            => throw new IOException("Injected PDB serialization failure.");
    }

    private sealed class PartialWriteFailureStream(string path)
        : FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)
    {
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count > 0)
                base.Write(buffer, offset, Math.Max(1, count / 2));
            throw new IOException("Injected partial PDB staging failure.");
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (!buffer.IsEmpty)
                base.Write(buffer[..Math.Max(1, buffer.Length / 2)]);
            throw new IOException("Injected partial PDB staging failure.");
        }
    }
}
