using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Lolcode.EndToEnd.Tests;

/// <summary>
/// Integration tests that run file-based and project-based samples using the
/// LOLCODE SDK.
/// </summary>
[Collection(nameof(SdkSampleCollection))]
public class SdkSampleTests
{
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string Configuration =
        new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;

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
        psi.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";
        psi.Environment["MSBUILDDISABLENODEREUSE"] = "1";

        using var process = Process.Start(psi)!;

        if (standardInput is not null)
        {
            process.StandardInput.Write(standardInput);
            process.StandardInput.Close();
        }

        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        Task completion = Task.WhenAll(process.WaitForExitAsync(), stdout, stderr);
        if (!completion.Wait(timeoutMs))
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException) when (process.HasExited)
            {
                // The process exited after the check but before Kill.
            }

            TimeSpan postKillDrainTimeout = TimeSpan.FromSeconds(5);
            Task postKillCompletion = Task.WhenAll(process.WaitForExitAsync(), stdout, stderr);
            postKillCompletion.Wait(postKillDrainTimeout);
            string capturedStdOut = GetCompletedOutput(stdout);
            string capturedStdErr = GetCompletedOutput(stderr);
            throw new TimeoutException(
                $"dotnet {args} did not complete within {TimeSpan.FromMilliseconds(timeoutMs).TotalSeconds:0} seconds " +
                $"(including stdout/stderr drain; post-kill drain waited up to {postKillDrainTimeout.TotalSeconds:0} seconds). " +
                $"Captured stdout:{Environment.NewLine}{capturedStdOut}{Environment.NewLine}" +
                $"Captured stderr:{Environment.NewLine}{capturedStdErr}");
        }
        completion.GetAwaiter().GetResult();

        return (process.ExitCode, stdout.Result, stderr.Result);
    }

    private static string GetCompletedOutput(Task<string> output) =>
        output.Status == TaskStatus.RanToCompletion
            ? output.GetAwaiter().GetResult()
            : "[output drain did not complete]";

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
        string command = $"run --project \"{projectFile}\"";
        if (projectFile.Replace('\\', '/').Contains(
                "can-has-galaxy/Galaxy.",
                StringComparison.Ordinal))
        {
            string projectName = Path.GetFileNameWithoutExtension(projectFile);
            command = $"\"{GetGalaxyAssemblyPath(projectName)}\"";
        }

        var (exitCode, stdout, stderr) = RunDotnet(
            command,
            RepoRoot,
            string.Empty);

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

    [Fact]
    public void CanHasGalaxySimulation_RunsDeterministicScenario()
    {
        AssertCommandOutput(
            $"\"{GetGalaxyAssemblyPath("Galaxy.Simulation")}\"",
            """
            CAN HAS GALAXY? SIMULATION v1
            CAPTAIN BOT | T0 | S0 | C6 | F6 | H9 | ORE 0
            SECTOR 1: NEBULA OF LASER POINTERZ | QUIET STARS. TEH RADIO PURRZ.
            MINED 1 ORE. HOLD IZ 1
            SOLD ORE FOR 3 CREDITZ.
            SECTOR 3: VOID OF UNSENT EMAIL | QUIET STARS. TEH RADIO PURRZ.
            WON TEH DOGFIGHT. HULL -2, BOUNTY +3.
            MISSION COMPLETE: TEH GALAXY REMEMBERS UR NAME.
            CAPTAIN BOT | T4 | S3 | C12 | F4 | H7 | ORE 0
            """);
    }

    [Fact]
    public void CanHasGalaxyGame_RunsScriptedSaveLoadSession()
    {
        string projectFile = Path.Combine(
            "samples",
            "project-based",
            "can-has-galaxy",
            "Galaxy.Game",
            "Galaxy.Game.lolproj");
        string assemblyPath = GetGalaxyAssemblyPath("Galaxy.Game");
        string workingDirectory = Path.Combine(
            Path.GetTempPath(),
            "can-has-galaxy-tests",
            Guid.NewGuid().ToString("N"));
        string saveFile = Path.Combine(workingDirectory, "can-has-galaxy.save");
        Directory.CreateDirectory(workingDirectory);

        try
        {
            File.Exists(assemblyPath).Should().BeTrue(
                $"the test project references {projectFile} as a build dependency");
            var (exitCode, stdout, stderr) = RunDotnet(
                $"\"{assemblyPath}\"",
                workingDirectory,
                "STATUS\nTRAVEL1\nMINE\nSAVE\nFIGHT\nSTATUS\nLOAD\nFIGHT\nSTATUS\nQUIT\n");

            exitCode.Should().Be(0, $"Galaxy game failed:\n{stderr}\n{stdout}");
            stdout.Should().Contain("=== CAN HAS GALAXY? ===");
            stdout.Should().Contain("SECTOR 1: NEBULA OF LASER POINTERZ");
            stdout.Should().Contain("MINED 1 ORE. HOLD IZ 1");
            stdout.Should().Contain("SAVE OK: can-has-galaxy.save");
            stdout.Should().Contain("LOAD OK.");
            stdout.Split(
                    "CAPTAIN CAPTAIN | T3 | S1 | C9 | F5 | H8 | ORE 1",
                    StringSplitOptions.None)
                .Should().HaveCount(3, "the same deterministic post-save action should produce the same state before and after loading");
            stdout.Should().Contain("KTHXBAI, CAPTAIN.");
            File.ReadAllText(saveFile).Should().StartWith("CHG2\n1\n6\n5\n9\n1\n0\n2\n7\n0\n");

            File.WriteAllText(
                saveFile,
                "BAD2\n99\n999\n99\n99\n999\n7\n-1\n7\n8\n");
            var (invalidExitCode, invalidStdout, invalidStderr) = RunDotnet(
                $"\"{assemblyPath}\"",
                workingDirectory,
                "LOAD\nQUIT\n");
            invalidExitCode.Should().Be(
                0,
                $"Galaxy invalid-save scenario failed:\n{invalidStderr}\n{invalidStdout}");
            invalidStdout.Should().Contain("NO VALID SAVE.");
        }
        finally
        {
            Directory.Delete(workingDirectory, recursive: true);
        }
    }

    private static string GetGalaxyAssemblyPath(string projectName) =>
        Path.Combine(
            RepoRoot,
            "samples",
            "project-based",
            "can-has-galaxy",
            projectName,
            "bin",
            Configuration,
            "net10.0",
            $"{projectName}.dll");

    private static void AssertProjectOutput(string projectFile, string expectedOutput)
        => AssertCommandOutput($"run --project \"{projectFile}\"", expectedOutput);

    private static void AssertCommandOutput(string command, string expectedOutput)
    {
        var (exitCode, stdout, stderr) = RunDotnet(
            command,
            RepoRoot);
        exitCode.Should().Be(0, $"dotnet command failed ({command}):\n{stderr}\n{stdout}");
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

    [Fact]
    public void SdkPack_ContainsStaticPropsAndBundledProviderAssemblies()
    {
        string sdkProject = Path.Combine(RepoRoot, "src", "Lolcode.NET.Sdk", "Lolcode.NET.Sdk.csproj");
        string template = Path.Combine(RepoRoot, "src", "Lolcode.NET.Sdk", "Sdk", "Sdk.props");
        string original = File.ReadAllText(template);
        string outputRoot = Path.Combine(Path.GetTempPath(), "lolcode-sdk-pack-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputRoot);

        try
        {
            const string version = "0.3.0-pack";
            var (exitCode, stdout, stderr) = RunDotnet(
                $"pack \"{sdkProject}\" --configuration Debug --no-build -p:PackageVersion={version} -o \"{outputRoot}\"",
                RepoRoot);
            exitCode.Should().Be(0, $"dotnet pack failed:\n{stderr}\n{stdout}");
            using ZipArchive package = ZipFile.OpenRead(
                Path.Combine(outputRoot, $"Lolcode.NET.Sdk.{version}.nupkg"));
            package.Entries.Select(entry => entry.FullName).Should().Contain(
            [
                "Sdk/Sdk.props",
                "Sdk/Sdk.targets",
                "tools/net10.0/Lolcode.Runtime.dll",
                "tools/net10.0/Lolcode.Runtime.String.dll",
                "tools/net10.0/Lolcode.Runtime.Stdlib.dll",
                "tools/net10.0/Lolcode.Runtime.Stdio.dll",
                "tools/net10.0/Lolcode.Runtime.Socks.dll",
            ]);
            using var reader = new StreamReader(package.GetEntry("Sdk/Sdk.props")!.Open());
            string contents = reader.ReadToEnd();
            contents.Should().NotContain("PackageVersion");
            File.ReadAllText(template).Should().Be(original);
        }
        finally
        {
            Directory.Delete(outputRoot, recursive: true);
        }
    }

    [Fact]
    public void Sdk_DoesNotInjectProviderPackageReferences()
    {
        string projectDirectory = CreateSdkTestDirectory("no-provider-packages");
        try
        {
            string sdkDirectory = Path.Combine(RepoRoot, "src", "Lolcode.NET.Sdk", "Sdk");
            string projectFile = Path.Combine(projectDirectory, "NoPackages.lolproj");
            File.WriteAllText(
                projectFile,
                $$"""
                <Project>
                  <Import Project="{{Path.Combine(sdkDirectory, "Sdk.props")}}" />
                  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
                  <Target Name="WritePackageReferences">
                    <WriteLinesToFile File="$(MSBuildProjectDirectory)/packages.txt"
                                      Lines="@(PackageReference->'%(Identity)')"
                                      Overwrite="true" />
                  </Target>
                  <Import Project="{{Path.Combine(sdkDirectory, "Sdk.targets")}}" />
                </Project>
                """);

            var (exitCode, stdout, stderr) = RunDotnet(
                $"msbuild \"{projectFile}\" -t:WritePackageReferences",
                projectDirectory);
            exitCode.Should().Be(0, $"package-reference inspection failed:\n{stderr}\n{stdout}");
            File.ReadAllText(Path.Combine(projectDirectory, "packages.txt")).Trim().Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    [Fact]
    [InlineLolcodeProgramException("The custom provider integration test writes a focused LOLCODE fixture.")]
    public void CustomProvider_WorksThroughProjectReferenceConvention()
    {
        string projectDirectory = CreateSdkTestDirectory("custom-provider-convention");
        try
        {
            string sdkDirectory = Path.Combine(RepoRoot, "src", "Lolcode.NET.Sdk", "Sdk");
            string buildTasksDirectory = Path.Combine(
                RepoRoot,
                "src",
                "Lolcode.Build",
                "bin",
                "Debug",
                "net10.0") + Path.DirectorySeparatorChar;
            string providerProject = Path.Combine(
                RepoRoot,
                "tests",
                "CustomProviderFixture",
                "CustomProviderFixture.csproj");
            string projectFile = Path.Combine(projectDirectory, "CustomConsumer.lolproj");
            File.WriteAllText(
                projectFile,
                $$"""
                <Project>
                  <Import Project="{{Path.Combine(sdkDirectory, "Sdk.props")}}" />
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net10.0</TargetFramework>
                    <_LolcodeBuildTasksDir>{{buildTasksDirectory}}</_LolcodeBuildTasksDir>
                  </PropertyGroup>
                  <ItemGroup>
                    <ProjectReference Include="{{providerProject}}" />
                  </ItemGroup>
                  <Import Project="{{Path.Combine(sdkDirectory, "Sdk.targets")}}" />
                </Project>
                """);
            File.WriteAllText(
                Path.Combine(projectDirectory, "Program.lol"),
                """
                HAI 1.4
                CAN HAS CUSTOM?
                VISIBLE I IZ CUSTOM'Z ECHO YR "HAI" MKAY
                KTHXBYE
                """);

            var (exitCode, stdout, stderr) = RunDotnet(
                $"run --project \"{projectFile}\"",
                projectDirectory);
            exitCode.Should().Be(0, $"custom provider build/run failed:\n{stderr}\n{stdout}");
            stdout.Trim().Should().Be("CUSTOM HAI");
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    [Fact]
    public void Publish_IncludesBundledProvidersWithoutTrimming()
    {
        string projectDirectory = CreateSdkTestDirectory("publish-bundled-providers");
        string publishDirectory = Path.Combine(projectDirectory, "publish");
        try
        {
            string projectFile = Path.Combine(
                RepoRoot,
                "samples",
                "project-based",
                "hello-world",
                "hello-world.lolproj");
            var (exitCode, stdout, stderr) = RunDotnet(
                $"publish \"{projectFile}\" --no-restore --configuration Debug -o \"{publishDirectory}\"",
                RepoRoot);
            exitCode.Should().Be(0, $"publish failed:\n{stderr}\n{stdout}");
            foreach (string provider in new[]
            {
                "Lolcode.Runtime.String.dll",
                "Lolcode.Runtime.Stdlib.dll",
                "Lolcode.Runtime.Stdio.dll",
                "Lolcode.Runtime.Socks.dll",
            })
            {
                File.Exists(Path.Combine(publishDirectory, provider)).Should().BeTrue();
            }
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
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
    [InlineData("\U00010400-cat", "__cat")]
    [InlineData("Lolcat-\U0001F431", "Lolcat__")]
    [InlineData("A\u200CB", "A_B")]
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
            string welcomeFixture = CompatibilityFixtures.ReadSource("DotNet/1.4/Sdk/library-welcome/test.lol");
            string projectFile = CreateDefaultLibraryProject(projectDirectory, assemblyName, welcomeFixture);

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

            string consumerProject = Path.Combine(projectDirectory, "Consumer.csproj");
            File.WriteAllText(
                consumerProject,
                $$"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net10.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <ProjectReference Include="{{projectFile}}" />
                  </ItemGroup>
                </Project>
                """);
            File.WriteAllText(
                Path.Combine(projectDirectory, "Program.cs"),
                $"using System;{Environment.NewLine}{Environment.NewLine}"
                + $"Console.WriteLine(InteropSamples.{expectedTypeName}.WELCOME(\"DOTNET\", 3));");

            var (consumerExitCode, consumerStdOut, consumerStdErr) = RunDotnet(
                $"run --project \"{consumerProject}\"",
                projectDirectory);
            consumerExitCode.Should().Be(
                0,
                $"C# consumer build failed:\n{consumerStdErr}\n{consumerStdOut}");
            consumerStdOut.Trim().Should().Be("HAI DOTNET, U CAN HAZ 3 CHEEZBURGERZ!");
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
                $"build \"{projectFile}\" --disable-build-servers -p:BuildInParallel=false",
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

    [Fact]
    public void ProjectNameEndingInLol_StillGlobsMultipleSources()
    {
        string projectDirectory = CreateSdkTestDirectory($"lol-project-name-{Guid.NewGuid():N}");

        try
        {
            string sdkDirectory = Path.Combine(RepoRoot, "src", "Lolcode.NET.Sdk", "Sdk");
            string buildTasksDirectory = Path.Combine(
                RepoRoot,
                "src",
                "Lolcode.Build",
                "bin",
                "Debug",
                "net10.0") + Path.DirectorySeparatorChar;
            string projectFile = Path.Combine(projectDirectory, "Example.lol.lolproj");
            File.WriteAllText(
                projectFile,
                $$"""
                <Project>
                  <Import Project="{{Path.Combine(sdkDirectory, "Sdk.props")}}" />
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net10.0</TargetFramework>
                    <_LolcodeBuildTasksDir>{{buildTasksDirectory}}</_LolcodeBuildTasksDir>
                  </PropertyGroup>
                  <Import Project="{{Path.Combine(sdkDirectory, "Sdk.targets")}}" />
                </Project>
                """);
            File.WriteAllText(
                Path.Combine(projectDirectory, "First.lol"),
                CompatibilityFixtures.ReadSource("DotNet/1.4/Sdk/multi-file-first/test.lol"));
            File.WriteAllText(
                Path.Combine(projectDirectory, "Second.lol"),
                CompatibilityFixtures.ReadSource("DotNet/1.4/Sdk/multi-file-second/test.lol"));

            var (exitCode, stdout, stderr) = RunDotnet(
                $"run --project \"{projectFile}\"",
                projectDirectory);

            exitCode.Should().Be(0, $"dotnet run failed:\n{stderr}\n{stdout}");
            stdout.Replace("\r\n", "\n").TrimEnd('\n').Should().Be("FIRST\nSECOND");
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    [Fact]
    public void Sdk_RebuildsWhenOnlyANonFirstLolSourceChanges()
    {
        string projectDirectory = Path.Combine(
            Path.GetTempPath(),
            "lolcode-sdk-multifile-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(projectDirectory);

        try
        {
            string sdkDirectory = Path.Combine(RepoRoot, "src", "Lolcode.NET.Sdk", "Sdk");
            string buildTasksDirectory = Path.Combine(
                RepoRoot,
                "src",
                "Lolcode.Build",
                "bin",
                "Debug",
                "net10.0") + Path.DirectorySeparatorChar;
            string projectFile = Path.Combine(projectDirectory, "MultiFile.lolproj");
            File.WriteAllText(
                projectFile,
                $$"""
                <Project>
                  <Import Project="{{Path.Combine(sdkDirectory, "Sdk.props")}}" />
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <AssemblyName>MultiFile</AssemblyName>
                    <_LolcodeBuildTasksDir>{{buildTasksDirectory}}</_LolcodeBuildTasksDir>
                  </PropertyGroup>
                  <ItemGroup>
                    <Compile Remove="**/*.lol" />
                    <Compile Include="01-First.lol" />
                    <Compile Include="02-Second.lol" />
                  </ItemGroup>
                  <Import Project="{{Path.Combine(sdkDirectory, "Sdk.targets")}}" />
                </Project>
                """);
            File.WriteAllText(
                Path.Combine(projectDirectory, "01-First.lol"),
                CompatibilityFixtures.ReadSource("DotNet/1.2/Sdk/incremental-first/test.lol"));
            string secondSource = Path.Combine(projectDirectory, "02-Second.lol");
            File.WriteAllText(
                secondSource,
                CompatibilityFixtures.ReadSource("DotNet/1.2/Sdk/incremental-second/test.lol"));

            var (firstExitCode, firstStdOut, firstStdErr) = RunDotnet(
                $"build \"{projectFile}\"",
                projectDirectory);
            firstExitCode.Should().Be(0, $"initial dotnet build failed:\n{firstStdErr}\n{firstStdOut}");

            File.AppendAllText(secondSource, "\nBTW changed non-first source");
            var (secondExitCode, secondStdOut, secondStdErr) = RunDotnet(
                $"build \"{projectFile}\" --no-restore --verbosity normal --disable-build-servers -p:BuildInParallel=false",
                projectDirectory);
            secondExitCode.Should().Be(0, $"incremental dotnet build failed:\n{secondStdErr}\n{secondStdOut}");
            secondStdOut.Should().Contain(
                "Lolc: Compiling 2 source file(s)",
                "CoreCompile inputs must include every Compile item.");
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    [Fact]
    public void Sdk_RebuildsWhenALolSourceIsDeleted()
    {
        string projectDirectory = CreateSdkTestDirectory("deleted-source");

        try
        {
            string functionFixture = CompatibilityFixtures.ReadSource("DotNet/1.4/Sdk/library-function-template/test.lol");
            string projectFile = WriteMultiFileLibraryProject(projectDirectory, useExplicitCompileItems: false);
            File.WriteAllText(Path.Combine(projectDirectory, "01-First.lol"), CreateLibraryFunction(functionFixture, "FIRST"));
            string secondSource = Path.Combine(projectDirectory, "02-Second.lol");
            File.WriteAllText(secondSource, CreateLibraryFunction("SECOND"));

            AssertBuildSucceeds(projectFile, projectDirectory, "initial build");
            string outputAssembly = GetSdkTestOutputAssembly(projectDirectory, "MultiFile");
            GetPublicMethodNames(outputAssembly, "Incremental", "MultiFile").Should().Contain(["FIRST", "SECOND"]);

            File.Delete(secondSource);
            var (exitCode, stdout, stderr) = RunDotnet(
                $"build \"{projectFile}\" --no-restore --verbosity normal",
                projectDirectory);
            exitCode.Should().Be(0, $"build after deleting a source failed:\n{stderr}\n{stdout}");
            stdout.Should().Contain("Lolc: Compiling 1 source file(s)");
            GetPublicMethodNames(outputAssembly, "Incremental", "MultiFile").Should().ContainSingle().Which.Should().Be("FIRST");
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    [Fact]
    public void Sdk_RebuildsWhenAnOlderLolSourceIsAdded()
    {
        string projectDirectory = CreateSdkTestDirectory("older-source");

        try
        {
            string projectFile = WriteMultiFileLibraryProject(projectDirectory, useExplicitCompileItems: false);
            string firstSource = Path.Combine(projectDirectory, "01-First.lol");
            File.WriteAllText(firstSource, CreateLibraryFunction("FIRST"));

            AssertBuildSucceeds(projectFile, projectDirectory, "initial build");
            string outputAssembly = GetSdkTestOutputAssembly(projectDirectory, "MultiFile");
            DateTime outputTime = File.GetLastWriteTimeUtc(outputAssembly);

            string secondSource = Path.Combine(projectDirectory, "02-Second.lol");
            File.WriteAllText(secondSource, CreateLibraryFunction("SECOND"));
            File.SetLastWriteTimeUtc(secondSource, outputTime.AddMinutes(-1));
            var (exitCode, stdout, stderr) = RunDotnet(
                $"build \"{projectFile}\" --no-restore --verbosity normal",
                projectDirectory);
            exitCode.Should().Be(0, $"build after adding an older source failed:\n{stderr}\n{stdout}");
            stdout.Should().Contain(
                "Lolc: Compiling 2 source file(s)",
                "the compilation-input stamp must account for a source added with an older timestamp.");
            GetPublicMethodNames(outputAssembly, "Incremental", "MultiFile").Should().Contain(["FIRST", "SECOND"]);
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    [Fact]
    public void Sdk_RebuildsWhenLolCompileOrderChanges()
    {
        string projectDirectory = CreateSdkTestDirectory("compile-order");

        try
        {
            string projectFile = WriteMultiFileLibraryProject(projectDirectory, ["01-First.lol", "02-Second.lol"]);
            File.WriteAllText(Path.Combine(projectDirectory, "01-First.lol"), CreateLibraryFunction("FIRST"));
            File.WriteAllText(Path.Combine(projectDirectory, "02-Second.lol"), CreateLibraryFunction("SECOND"));

            AssertBuildSucceeds(projectFile, projectDirectory, "initial build");
            string outputAssembly = GetSdkTestOutputAssembly(projectDirectory, "MultiFile");
            GetPublicMethodNames(outputAssembly, "Incremental", "MultiFile").Should().Equal("FIRST", "SECOND");

            WriteMultiFileLibraryProject(projectDirectory, ["02-Second.lol", "01-First.lol"]);
            var (exitCode, stdout, stderr) = RunDotnet(
                $"build \"{projectFile}\" --no-restore --verbosity normal",
                projectDirectory);
            exitCode.Should().Be(0, $"build after changing Compile order failed:\n{stderr}\n{stdout}");
            stdout.Should().Contain("Lolc: Compiling 2 source file(s)");
            GetPublicMethodNames(outputAssembly, "Incremental", "MultiFile").Should().Equal("SECOND", "FIRST");
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    [Theory]
    [InlineData("hyphenated-library", "hyphenated_library")]
    [InlineData("2leading-library", "_2leading_library")]
    public void LolcodeLibrary_DerivesSanitizedRootNamespaceConsumableFromCSharp(
        string assemblyName,
        string expectedIdentifier)
    {
        string projectDirectory = CreateSdkTestDirectory($"sanitized-root-{assemblyName}");

        try
        {
            string libraryProject = WriteMultiFileLibraryProject(
                projectDirectory,
                ["Exports.lol"],
                assemblyName,
                rootNamespace: null);
            File.WriteAllText(Path.Combine(projectDirectory, "Exports.lol"), CreateLibraryFunction("FIRST"));

            AssertBuildSucceeds(libraryProject, projectDirectory, "LOLCODE library build");
            string outputAssembly = GetSdkTestOutputAssembly(projectDirectory, assemblyName);
            AssertAssemblyContainsType(outputAssembly, expectedIdentifier, expectedIdentifier);

            string consumerProject = Path.Combine(projectDirectory, "Consumer.csproj");
            File.WriteAllText(
                consumerProject,
                $$"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net10.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <ProjectReference Include="{{libraryProject}}" />
                  </ItemGroup>
                </Project>
                """);
            File.WriteAllText(
                Path.Combine(projectDirectory, "Program.cs"),
                $"Console.WriteLine({expectedIdentifier}.{expectedIdentifier}.FIRST());");

            var (exitCode, stdout, stderr) = RunDotnet(
                $"run --project \"{consumerProject}\" --no-restore",
                projectDirectory);
            exitCode.Should().Be(0, $"C# consumer build failed:\n{stderr}\n{stdout}");
            stdout.Trim().Should().Be("FIRST");
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    [Theory]
    [InlineData("DefaultNamespace", null, "DefaultNamespace")]
    [InlineData("RepeatedNamespace", "Alpha.Alpha", "Alpha.Alpha")]
    [InlineData("RepeatedNamespace", "Alpha.Beta.Alpha", "Alpha.Beta.Alpha")]
    [InlineData("RepeatedNamespace", "Alpha.2-Beta.Alpha.2-Beta", "Alpha._2_Beta.Alpha._2_Beta")]
    public void LolcodeLibrary_PreservesEverySanitizedRootNamespaceSegmentConsumableFromCSharp(
        string assemblyName,
        string? rootNamespace,
        string expectedNamespace)
    {
        string projectDirectory = CreateSdkTestDirectory($"repeated-root-{Guid.NewGuid():N}");

        try
        {
            string libraryProject = WriteMultiFileLibraryProject(
                projectDirectory,
                ["Exports.lol"],
                assemblyName,
                rootNamespace);
            File.WriteAllText(Path.Combine(projectDirectory, "Exports.lol"), CreateLibraryFunction("FIRST"));

            AssertBuildSucceeds(libraryProject, projectDirectory, "LOLCODE library build");
            string outputAssembly = GetSdkTestOutputAssembly(projectDirectory, assemblyName);
            AssertAssemblyContainsType(outputAssembly, expectedNamespace, assemblyName);

            string consumerProject = Path.Combine(projectDirectory, "Consumer.csproj");
            File.WriteAllText(
                consumerProject,
                $$"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net10.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <ProjectReference Include="{{libraryProject}}" />
                  </ItemGroup>
                </Project>
                """);
            File.WriteAllText(
                Path.Combine(projectDirectory, "Program.cs"),
                $"Console.WriteLine({expectedNamespace}.{assemblyName}.FIRST());");

            var (exitCode, stdout, stderr) = RunDotnet(
                $"run --project \"{consumerProject}\" --no-restore",
                projectDirectory);
            exitCode.Should().Be(0, $"C# consumer build failed:\n{stderr}\n{stdout}");
            stdout.Trim().Should().Be("FIRST");
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    [Theory]
    [InlineData("RootlessDefault", null, false)]
    [InlineData("RootlessSpecified", "Exports", true)]
    public void LolcodeLibrary_UsesExplicitEmptyRootNamespaceConsumableFromCSharp(
        string assemblyName,
        string? libraryTypeName,
        bool useCommandLineRootNamespace)
    {
        string projectDirectory = CreateSdkTestDirectory($"rootless-{assemblyName}");
        string expectedTypeName = libraryTypeName ?? assemblyName;

        try
        {
            string libraryProject = WriteMultiFileLibraryProject(
                projectDirectory,
                ["Exports.lol"],
                assemblyName,
                rootNamespace: useCommandLineRootNamespace ? null : string.Empty,
                libraryTypeName: libraryTypeName);
            File.WriteAllText(Path.Combine(projectDirectory, "Exports.lol"), CreateLibraryFunction("FIRST"));

            if (useCommandLineRootNamespace)
            {
                var (libraryExitCode, libraryStdout, libraryStderr) = RunDotnet(
                    $"build \"{libraryProject}\" -p:RootNamespace=",
                    projectDirectory);
                libraryExitCode.Should().Be(
                    0,
                    $"rootless command-line LOLCODE library build failed:\n{libraryStderr}\n{libraryStdout}");
            }
            else
            {
                AssertBuildSucceeds(libraryProject, projectDirectory, "rootless LOLCODE library build");
            }
            string outputAssembly = GetSdkTestOutputAssembly(projectDirectory, assemblyName);
            AssertAssemblyContainsType(outputAssembly, string.Empty, expectedTypeName);

            string consumerProject = Path.Combine(projectDirectory, "Consumer.csproj");
            File.WriteAllText(
                consumerProject,
                $$"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net10.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <ProjectReference Include="{{libraryProject}}" />
                  </ItemGroup>
                </Project>
                """);
            File.WriteAllText(
                Path.Combine(projectDirectory, "Program.cs"),
                $"Console.WriteLine({expectedTypeName}.FIRST());");

            string rootNamespaceArgument = useCommandLineRootNamespace
                ? " -p:RootNamespace="
                : string.Empty;
            var (consumerBuildExitCode, consumerBuildStdout, consumerBuildStderr) = RunDotnet(
                $"build \"{consumerProject}\" --no-restore{rootNamespaceArgument}",
                projectDirectory);
            consumerBuildExitCode.Should().Be(
                0,
                $"rootless C# consumer build failed:\n{consumerBuildStderr}\n{consumerBuildStdout}");
            var (exitCode, stdout, stderr) = RunDotnet(
                $"run --project \"{consumerProject}\" --no-build",
                projectDirectory);
            exitCode.Should().Be(0, $"rootless C# consumer run failed:\n{stderr}\n{stdout}");
            stdout.Trim().Should().Be("FIRST");
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    [Fact]
    public void Sdk_ProjectReferenceRunsWhenBuildOutputsAreRedirected()
    {
        string projectDirectory = CreateSdkTestDirectory("redirected-project-reference");

        try
        {
            string libraryDirectory = Path.Combine(projectDirectory, "Library");
            string consumerDirectory = Path.Combine(projectDirectory, "Consumer");
            Directory.CreateDirectory(libraryDirectory);
            Directory.CreateDirectory(consumerDirectory);

            string libraryProject = WriteMultiFileLibraryProject(
                libraryDirectory,
                ["Exports.lol"],
                assemblyName: "RedirectedLibrary",
                rootNamespace: string.Empty,
                additionalProperties:
                    "    <BaseOutputPath>$(MSBuildProjectDirectory)/published/</BaseOutputPath>" +
                    Environment.NewLine);
            File.WriteAllText(
                Path.Combine(libraryDirectory, "Exports.lol"),
                CreateLibraryFunction("HELLO"));

            string sdkDirectory = Path.Combine(RepoRoot, "src", "Lolcode.NET.Sdk", "Sdk");
            string buildTasksDirectory = Path.Combine(
                RepoRoot,
                "src",
                "Lolcode.Build",
                "bin",
                "Debug",
                "net10.0") + Path.DirectorySeparatorChar;
            string consumerProject = Path.Combine(consumerDirectory, "Consumer.lolproj");
            File.WriteAllText(
                consumerProject,
                $$"""
                <Project>
                  <Import Project="{{Path.Combine(sdkDirectory, "Sdk.props")}}" />
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net10.0</TargetFramework>
                    <AssemblyName>RedirectedConsumer</AssemblyName>
                    <BaseOutputPath>$(MSBuildProjectDirectory)/published/</BaseOutputPath>
                    <_LolcodeBuildTasksDir>{{buildTasksDirectory}}</_LolcodeBuildTasksDir>
                  </PropertyGroup>
                  <ItemGroup>
                    <ProjectReference Include="{{libraryProject}}" />
                  </ItemGroup>
                  <Import Project="{{Path.Combine(sdkDirectory, "Sdk.targets")}}" />
                </Project>
                """);
            File.WriteAllText(
                Path.Combine(consumerDirectory, "Program.lol"),
                CompatibilityFixtures.ReadSource("DotNet/1.4/Sdk/redirected-consumer/test.lol"));

            var (buildExitCode, buildStdout, buildStderr) = RunDotnet(
                $"build \"{consumerProject}\"",
                consumerDirectory);
            buildExitCode.Should().Be(
                0,
                $"redirected ProjectReference build failed:\n{buildStderr}\n{buildStdout}");

            string outputDirectory = Path.Combine(consumerDirectory, "published", "Debug", "net10.0");
            File.Exists(Path.Combine(outputDirectory, "RedirectedConsumer.dll")).Should().BeTrue();
            File.Exists(Path.Combine(outputDirectory, "RedirectedLibrary.dll")).Should().BeTrue();
            File.Exists(Path.Combine(
                libraryDirectory,
                "bin",
                "Debug",
                "net10.0",
                "RedirectedLibrary.dll")).Should().BeFalse();

            var (runExitCode, runStdout, runStderr) = RunDotnet(
                $"run --project \"{consumerProject}\" --no-build",
                consumerDirectory);
            runExitCode.Should().Be(0, $"redirected ProjectReference run failed:\n{runStderr}\n{runStdout}");
            runStdout.Trim().Should().Be("HELLO");
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    private static string CreateSdkTestDirectory(string name)
    {
        string projectDirectory = Path.Combine(
            RepoRoot,
            "artifacts",
            "sdk-e2e-tests",
            name,
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(projectDirectory);
        return projectDirectory;
    }

    private static string WriteMultiFileLibraryProject(
        string projectDirectory,
        IEnumerable<string>? sourceFiles = null,
        string assemblyName = "MultiFile",
        string? rootNamespace = "Incremental",
        bool useExplicitCompileItems = true,
        string? libraryTypeName = null,
        string? additionalProperties = null)
    {
        string sdkDirectory = Path.Combine(RepoRoot, "src", "Lolcode.NET.Sdk", "Sdk");
        string buildTasksDirectory = Path.Combine(
            RepoRoot,
            "src",
            "Lolcode.Build",
            "bin",
            "Debug",
            "net10.0") + Path.DirectorySeparatorChar;
        string projectFile = Path.Combine(projectDirectory, $"{assemblyName}.lolproj");
        string rootNamespaceProperty = rootNamespace is null
            ? ""
            : $"    <RootNamespace>{rootNamespace}</RootNamespace>{Environment.NewLine}";
        string libraryTypeNameProperty = libraryTypeName is null
            ? ""
            : $"    <LolcodeLibraryTypeName>{libraryTypeName}</LolcodeLibraryTypeName>{Environment.NewLine}";
        additionalProperties ??= string.Empty;
        string compileItemGroup = useExplicitCompileItems
            ? "  <ItemGroup>" + Environment.NewLine +
              "    <Compile Remove=\"**/*.lol\" />" + Environment.NewLine +
              string.Join(
                  Environment.NewLine,
                  (sourceFiles ?? Enumerable.Empty<string>())
                      .Select(source => $"    <Compile Include=\"{source}\" />")) + Environment.NewLine +
              "  </ItemGroup>"
            : "";
        File.WriteAllText(
            projectFile,
            $$"""
            <Project>
              <Import Project="{{Path.Combine(sdkDirectory, "Sdk.props")}}" />
              <PropertyGroup>
                <OutputType>Library</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <AssemblyName>{{assemblyName}}</AssemblyName>
            {{rootNamespaceProperty}}{{libraryTypeNameProperty}}{{additionalProperties}}
                <_LolcodeBuildTasksDir>{{buildTasksDirectory}}</_LolcodeBuildTasksDir>
              </PropertyGroup>
            {{compileItemGroup}}
              <Import Project="{{Path.Combine(sdkDirectory, "Sdk.targets")}}" />
            </Project>
            """);
        return projectFile;
    }

    private static string CreateLibraryFunction(string name) =>
        CreateLibraryFunction(CompatibilityFixtures.ReadSource("DotNet/1.4/Sdk/library-function-template/test.lol"), name);

    private static string CreateLibraryFunction(string template, string name) =>
        template.Replace("{{name}}", name, StringComparison.Ordinal);

    private static void AssertBuildSucceeds(string projectFile, string projectDirectory, string operation)
    {
        var (exitCode, stdout, stderr) = RunDotnet(
            $"build \"{projectFile}\"",
            projectDirectory);
        exitCode.Should().Be(0, $"{operation} failed:\n{stderr}\n{stdout}");
    }

    private static string GetSdkTestOutputAssembly(string projectDirectory, string assemblyName) =>
        Path.Combine(projectDirectory, "obj", "Debug", "net10.0", $"{assemblyName}.dll");

    private static IReadOnlyList<string> GetPublicMethodNames(
        string outputAssembly,
        string expectedNamespace,
        string expectedTypeName)
    {
        using var stream = File.OpenRead(outputAssembly);
        using var peReader = new PEReader(stream);
        MetadataReader metadata = peReader.GetMetadataReader();
        TypeDefinition type = metadata.TypeDefinitions
            .Select(metadata.GetTypeDefinition)
            .Single(definition =>
                metadata.GetString(definition.Namespace) == expectedNamespace &&
                metadata.GetString(definition.Name) == expectedTypeName);
        return type.GetMethods()
            .Select(metadata.GetMethodDefinition)
            .Where(method => (method.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public)
            .Select(method => metadata.GetString(method.Name))
            .Where(name => name != "__CreateLolcodeLibrary")
            .ToArray();
    }

    private static string CreateDefaultLibraryProject(string projectDirectory, string assemblyName, string? welcomeSource = null)
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
                <_LolcodeBuildTasksDir>{{buildTasksDirectory}}</_LolcodeBuildTasksDir>
              </PropertyGroup>
              <Import Project="{{Path.Combine(sdkDirectory, "Sdk.targets")}}" />
            </Project>
            """);
        File.WriteAllText(
            Path.Combine(projectDirectory, "Welcome.lol"),
            welcomeSource ?? CompatibilityFixtures.ReadSource("DotNet/1.4/Sdk/library-welcome/test.lol"));
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
        var (exitCode, stdout, stderr) = RunDotnet("run --file hello.lol --verbosity diagnostic", sampleDir);

        exitCode.Should().Be(0, $"dotnet run --file failed:\n{stderr}");

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

/// <summary>
/// Serializes nested SDK builds because they share source-built compiler and provider output paths.
/// </summary>
[CollectionDefinition(nameof(SdkSampleCollection), DisableParallelization = true)]
public sealed class SdkSampleCollection;
