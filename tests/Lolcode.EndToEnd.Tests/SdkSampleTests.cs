using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Lolcode.EndToEnd.Tests;

/// <summary>
/// Integration tests that run file-based and project-based samples using the
/// LOLCODE SDK.
/// </summary>
public class SdkSampleTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    private static string FindRepoRoot()
    {
        string dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "dotnet-lolcode.slnx")))
                return dir;
            dir = Path.GetDirectoryName(dir)!;
        }
        throw new InvalidOperationException("Could not find repo root (looked for dotnet-lolcode.slnx)");
    }

    private static (int ExitCode, string StdOut, string StdErr) RunDotnet(
        string args,
        string workingDir,
        string? standardInput = null,
        int timeoutMs = 30_000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = args,
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = standardInput is not null,
        };

        using var process = Process.Start(psi)!;

        if (standardInput is not null)
        {
            process.StandardInput.Write(standardInput);
            process.StandardInput.Close();
        }
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(timeoutMs);

        return (process.ExitCode, stdout, stderr);
    }

    /// <summary>
    /// Discovers the primary file-based sample catalog.
    /// </summary>
    public static IEnumerable<object[]> GetFileBasedSamples()
    {
        string samplesDir = Path.Combine(RepoRoot, "samples");
        foreach (string sourceFile in Directory.EnumerateFiles(samplesDir, "*.lol", SearchOption.AllDirectories)
            .Where(path => !path.StartsWith(Path.Combine(samplesDir, "project-based"), StringComparison.Ordinal))
            .Order())
        {
            string relativePath = Path.GetRelativePath(RepoRoot, sourceFile);
            yield return [relativePath];
        }
    }

    [Theory]
    [MemberData(nameof(GetFileBasedSamples))]
    public void FileBasedSample_Runs(string sampleFile)
    {
        var fullPath = Path.Combine(RepoRoot, sampleFile);
        var standardInput = Path.GetFileName(fullPath) switch
        {
            "guess.lol" => "42\n",
            "adventure.lol" => "quit\n",
            "Game.lol" => "TESTER\nflee\n",
            "chess.lol" => "quit\n",
            "tic-tac-toe.lol" => "1\nq\n",
            "calculator.lol" => "quit\n",
            "truth-machine.lol" => "0\n",
            _ => null,
        };
        var (exitCode, stdout, stderr) = RunDotnet(
            $"run --file \"{Path.GetFileName(fullPath)}\"",
            Path.GetDirectoryName(fullPath)!,
            standardInput,
            timeoutMs: 60_000);

        exitCode.Should().Be(0, $"dotnet run --file failed for {sampleFile}:\n{stderr}\n{stdout}");
    }

    /// <summary>Discovers executable project-based samples.</summary>
    public static IEnumerable<object[]> GetProjectBasedExecutableSamples()
    {
        return Directory
            .EnumerateFiles(Path.Combine(RepoRoot, "samples"), "*.lolproj", SearchOption.AllDirectories)
            .Where(project => !File.ReadAllText(project).Contains(
                "<OutputType>Library</OutputType>",
                StringComparison.Ordinal))
            .Order()
            .Select(project => new object[] { Path.GetRelativePath(RepoRoot, project) });
    }

    [Theory]
    [MemberData(nameof(GetProjectBasedExecutableSamples))]
    public void ProjectBasedExecutableSample_Runs(string projectFile)
    {
        var (exitCode, stdout, stderr) = RunDotnet(
            $"run --project \"{projectFile}\"",
            RepoRoot);

        exitCode.Should().Be(0, $"dotnet run --project failed for {projectFile}:\n{stderr}\n{stdout}");
    }

    [Fact]
    public void ProjectBasedHelloWorld_Runs_CorrectOutput()
    {
        AssertProjectOutput(
            "samples/project-based/hello-world/hello-world.lolproj",
            "HAI WORLD FROM A LOLPROJ!");
    }

    [Fact]
    public void LolcodeHead_CallsManagedTextPackage()
    {
        AssertProjectOutput(
            "samples/project-based/mixed-language/lolcode-head-csharp-library/LolcodeHead/LolcodeHead.lolproj",
            "HAI HAI HAI\n11");
    }

    [Fact]
    public void LolcodeHead_CallsMarkedLolcodeLibrary()
    {
        AssertProjectOutput(
            "samples/project-based/lolcode-head-lolcode-library/LolcodeHead/LolcodeHead.lolproj",
            "HAI FROM LOLCODE LIBRARY!\n1\n2");
    }

    private static void AssertProjectOutput(string projectFile, string expectedOutput)
    {
        var (exitCode, stdout, stderr) = RunDotnet(
            $"run --project \"{projectFile}\"",
            RepoRoot);
        exitCode.Should().Be(0, $"dotnet run --project failed for {projectFile}:\n{stderr}\n{stdout}");
        var output = stdout.Replace("\r\n", "\n").TrimEnd('\n');
        output.Should().Be(expectedOutput);
    }

    [Fact]
    public void CSharpHeadLolcodeLibrarySample_Runs_CorrectOutput()
    {
        string projectFile = Path.Combine(
            RepoRoot,
            "samples",
            "project-based",
            "csharp-head-lolcode-library",
            "CSharpHead",
            "CSharpHead.csproj");

        var (exitCode, stdout, stderr) = RunDotnet(
            $"run --project \"{projectFile}\"",
            RepoRoot);

        exitCode.Should().Be(0, $"dotnet run --project failed:\n{stderr}");

        var output = stdout.Replace("\r\n", "\n").TrimEnd('\n');
        output.Should().Be("HAI DOTNET, U CAN HAZ 3 CHEEZBURGERZ!\n4\nHAI FROM A LIBRARY SCOPE!");
    }

    [Theory]
    [InlineData("InteropSamples.LolcatExports")]
    [InlineData("Lolcat-Exports")]
    public void CSharpHeadLolcodeLibrarySample_RejectsInvalidLibraryTypeName(string libraryTypeName)
    {
        string projectFile = Path.Combine(
            RepoRoot,
            "samples",
            "project-based",
            "csharp-head-lolcode-library",
            "LolcatPhraseLibrary",
            "LolcatPhraseLibrary.lolproj");

        var (exitCode, stdout, stderr) = RunDotnet(
            $"build \"{projectFile}\" -p:LolcodeLibraryTypeName={libraryTypeName}",
            RepoRoot);

        exitCode.Should().NotBe(0);
        $"{stdout}\n{stderr}".Should().Contain(
            "LolcodeLibraryTypeName must be a simple, non-keyword CLR/C# identifier without dots.");
    }

    [Theory]
    [InlineData("Lolcat-Phrase", "Lolcat_Phrase")]
    [InlineData("2Cats", "_2Cats")]
    [InlineData("class", "_class")]
    [InlineData("\U00010400-cat", "\U00010400_cat")]
    [InlineData("Lolcat-\U0001F431", "Lolcat__")]
    public void LolcodeLibrary_DefaultExportTypeName_IsSanitizedFromAssemblyName(
        string assemblyName,
        string expectedTypeName)
    {
        string projectDirectory = Path.Combine(
            Path.GetTempPath(),
            "lolcode-sdk-sample-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(projectDirectory);

        try
        {
            string projectFile = CreateDefaultLibraryProject(projectDirectory, assemblyName);

            var (exitCode, stdout, stderr) = RunDotnet(
                $"build \"{projectFile}\"",
                projectDirectory);

            exitCode.Should().Be(0, $"dotnet build failed:\n{stderr}\n{stdout}");

            string outputAssembly = Path.Combine(
                projectDirectory,
                "obj",
                "Debug",
                "net10.0",
                $"{assemblyName}.dll");
            AssertAssemblyContainsType(outputAssembly, "InteropSamples", expectedTypeName);
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    [Fact]
    public void LolcodeLibrary_ChangingExportTypeName_RebuildsAssembly()
    {
        string projectDirectory = Path.Combine(
            Path.GetTempPath(),
            "lolcode-sdk-sample-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(projectDirectory);

        try
        {
            string assemblyName = "Lolcat-Phrase";
            string projectFile = CreateDefaultLibraryProject(projectDirectory, assemblyName);

            var (firstExitCode, firstStdOut, firstStdErr) = RunDotnet(
                $"build \"{projectFile}\"",
                projectDirectory);
            firstExitCode.Should().Be(0, $"initial dotnet build failed:\n{firstStdErr}\n{firstStdOut}");

            var (secondExitCode, secondStdOut, secondStdErr) = RunDotnet(
                $"build \"{projectFile}\" -p:LolcodeLibraryTypeName=ChangedExports",
                projectDirectory);
            secondExitCode.Should().Be(0, $"updated dotnet build failed:\n{secondStdErr}\n{secondStdOut}");

            string outputAssembly = Path.Combine(
                projectDirectory,
                "obj",
                "Debug",
                "net10.0",
                $"{assemblyName}.dll");
            AssertAssemblyContainsType(outputAssembly, "InteropSamples", "ChangedExports");
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    [Fact]
    public void LolcodeLibrary_ChangingOutputTypeToExecutable_RebuildsAssembly()
    {
        string projectDirectory = Path.Combine(
            Path.GetTempPath(),
            "lolcode-sdk-sample-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(projectDirectory);

        try
        {
            string assemblyName = "Lolcat-Phrase";
            string projectFile = CreateDefaultLibraryProject(projectDirectory, assemblyName);

            var (libraryExitCode, libraryStdOut, libraryStdErr) = RunDotnet(
                $"build \"{projectFile}\"",
                projectDirectory);
            libraryExitCode.Should().Be(0, $"library build failed:\n{libraryStdErr}\n{libraryStdOut}");

            var (executableExitCode, executableStdOut, executableStdErr) = RunDotnet(
                $"build \"{projectFile}\" -p:OutputType=Exe",
                projectDirectory);
            executableExitCode.Should().Be(
                0,
                $"executable build failed:\n{executableStdErr}\n{executableStdOut}");

            string outputAssembly = Path.Combine(
                projectDirectory,
                "obj",
                "Debug",
                "net10.0",
                $"{assemblyName}.dll");
            using var stream = File.OpenRead(outputAssembly);
            using var peReader = new PEReader(stream);
            peReader.PEHeaders.CoffHeader.Characteristics.Should().NotHaveFlag(Characteristics.Dll);
            peReader.PEHeaders.CorHeader!.EntryPointTokenOrRelativeVirtualAddress.Should().NotBe(0);
            File.Exists(Path.ChangeExtension(outputAssembly, ".runtimeconfig.json")).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    private static string CreateDefaultLibraryProject(string projectDirectory, string assemblyName)
    {
        string sdkDirectory = Path.Combine(RepoRoot, "src", "Lolcode.NET.Sdk", "Sdk");
        string buildTasksDirectory = Path.Combine(
            RepoRoot,
            "src",
            "Lolcode.Build",
            "bin",
            "Debug",
            "net10.0") + Path.DirectorySeparatorChar;
        string projectFile = Path.Combine(projectDirectory, "LolcatPhrase.lolproj");
        File.WriteAllText(
            projectFile,
            $$"""
            <Project>
              <Import Project="{{Path.Combine(sdkDirectory, "Sdk.props")}}" />
              <PropertyGroup>
                <OutputType>Library</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <AssemblyName>{{assemblyName}}</AssemblyName>
                <RootNamespace>InteropSamples</RootNamespace>
                <LolcodeUseDefaultLibraries>false</LolcodeUseDefaultLibraries>
                <_LolcodeBuildTasksDir>{{buildTasksDirectory}}</_LolcodeBuildTasksDir>
              </PropertyGroup>
              <Import Project="{{Path.Combine(sdkDirectory, "Sdk.targets")}}" />
            </Project>
            """);
        File.WriteAllText(
            Path.Combine(projectDirectory, "Welcome.lol"),
            """
            HAI 1.4
            HOW IZ I WELCOME
                FOUND YR "HAI"
            IF U SAY SO
            KTHXBYE
            """);
        return projectFile;
    }

    private static void AssertAssemblyContainsType(
        string outputAssembly,
        string expectedNamespace,
        string expectedTypeName)
    {
        using var stream = File.OpenRead(outputAssembly);
        using var peReader = new PEReader(stream);
        MetadataReader metadata = peReader.GetMetadataReader();
        metadata.TypeDefinitions
            .Select(metadata.GetTypeDefinition)
            .Should()
            .Contain(definition =>
                metadata.GetString(definition.Namespace) == expectedNamespace &&
                metadata.GetString(definition.Name) == expectedTypeName);
    }

    [Fact]
    public void FileBasedHelloWorld_BuildsWithoutRunning()
    {
        var sampleDir = Path.Combine(RepoRoot, "samples", "basics", "hello-world");
        var (exitCode, stdout, stderr) = RunDotnet("build hello.lol", sampleDir);

        exitCode.Should().Be(0, $"dotnet build failed:\n{stderr}\n{stdout}");
    }

    [Fact]
    public void FileBasedHelloWorld_Runs_CorrectOutput()
    {
        var sampleDir = Path.Combine(RepoRoot, "samples", "basics", "hello-world");
        var localBuildTasksDir = Path.Combine(RepoRoot, "src", "Lolcode.Build", "bin", "Debug", "net10.0");
        var (exitCode, stdout, stderr) = RunDotnet("run --file hello.lol --verbosity diagnostic", sampleDir);

        exitCode.Should().Be(0, $"dotnet run --file failed:\n{stderr}");
        stdout.Should().Contain(localBuildTasksDir, "file-based samples should use the source-built compiler");

        var output = stdout.Replace("\r\n", "\n").TrimEnd('\n');
        output.Should().EndWith("HAI WORLD!");
    }

    [Fact]
    public void FileBasedHelloWorld_ReportsMissingLocalCompiler()
    {
        var sampleDir = Path.Combine(RepoRoot, "samples", "basics", "hello-world");
        var (exitCode, stdout, stderr) = RunDotnet(
            "run --file hello.lol --configuration MissingLocalCompiler",
            sampleDir);

        exitCode.Should().NotBe(0);
        $"{stdout}\n{stderr}".Should().Contain(
            "The source-built LOLCODE compiler was not found",
            "file-based samples must never fall back to the packaged compiler");
    }

    [Fact]
    public void Chess_Runs_PlayerAndAiTurn()
    {
        var sampleDir = Path.Combine(RepoRoot, "samples", "games", "chess");
        var (exitCode, stdout, stderr) = RunDotnet(
            "run --file chess.lol",
            sampleDir,
            "e2\ne4\nquit\n",
            timeoutMs: 60_000);

        exitCode.Should().Be(0, $"dotnet run --file failed:\n{stderr}");
        stdout.Should().Contain("8 | r n b q k b n r | 8");
        stdout.Should().Contain("1 | R N B Q K B N R | 1");
        stdout.Should().Contain("AI MOVE:");
        stdout.Should().Contain("4 | . . . . P . . . | 4");
        stdout.Should().Contain("KTHXBAI! TANKS 4 PLAYIN LOLCHESS!");
    }

    [Fact]
    public void TicTacToe_TwoPlayerMode_RunsToPlayerWin()
    {
        var sampleDir = Path.Combine(RepoRoot, "samples", "games", "tic-tac-toe");
        var (exitCode, stdout, stderr) = RunDotnet(
            "run --file tic-tac-toe.lol",
            sampleDir,
            "2\n1\n4\n2\n5\n3\n",
            timeoutMs: 60_000);

        exitCode.Should().Be(0, $"dotnet run --file failed:\n{stderr}");
        stdout.Should().Contain("=== LOLCODE TIC-TAC-TOE ===");
        stdout.Split("=== LOLCODE TIC-TAC-TOE ===", StringSplitOptions.None)
            .Should().HaveCount(2, "the title should appear exactly once");
        stdout.IndexOf("HOW MANY PLAYERZ?", StringComparison.Ordinal)
            .Should().BeLessThan(stdout.IndexOf("PICK A CELL", StringComparison.Ordinal));
        stdout.IndexOf("HOW MANY PLAYERZ?", StringComparison.Ordinal)
            .Should().BeLessThan(stdout.IndexOf(" 1 | 2 | 3", StringComparison.Ordinal));
        stdout.Should().Contain(" 1 | 2 | 3");
        stdout.Should().Contain("   |   |  ");
        stdout.Should().Contain(" X | X | X");
        stdout.Should().Contain("PLAYER X WINZ!");
    }

    [Fact]
    public void TicTacToe_OnePlayerMode_RunsAiTurn()
    {
        var sampleDir = Path.Combine(RepoRoot, "samples", "games", "tic-tac-toe");
        var (exitCode, stdout, stderr) = RunDotnet(
            "run --file tic-tac-toe.lol",
            sampleDir,
            "1\n1\n2\n7\n9\n",
            timeoutMs: 60_000);

        exitCode.Should().Be(0, $"dotnet run --file failed:\n{stderr}");
        stdout.Should().Contain("U R X. TEH AI IZ O.");
        stdout.Should().Contain("TEH AI PICKZ CELL 5!");
        stdout.Should().Contain("TEH AI PICKZ CELL 3!");
        stdout.Should().Contain(" O | O | O");
        stdout.Should().Contain("TEH AI WINZ!");
    }
}
