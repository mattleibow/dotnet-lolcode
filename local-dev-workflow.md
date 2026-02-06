# Local SDK Development Workflow

## Quick Start

1. **Build everything:**
   ```bash
   dotnet build dotnet-lolcode.slnx
   ```

2. **Create a local NuGet package:**
   ```bash
   # Create a temp directory for the package
   mkdir -p /tmp/lolcode-feed

   # Build the SDK components
   dotnet build src/Lolcode.Build/Lolcode.Build.csproj -c Debug

   # Create the NuGet package
   mkdir -p /tmp/lolcode-pkg/Sdk /tmp/lolcode-pkg/tools/net10.0
   cp src/Lolcode.NET.Sdk/Sdk/Sdk.props src/Lolcode.NET.Sdk/Sdk/Sdk.targets /tmp/lolcode-pkg/Sdk/
   cp src/Lolcode.Build/bin/Debug/net10.0/Lolcode.Build.dll /tmp/lolcode-pkg/tools/net10.0/
   cp src/Lolcode.Build/bin/Debug/net10.0/Lolcode.Build.deps.json /tmp/lolcode-pkg/tools/net10.0/
   cp src/Lolcode.Build/bin/Debug/net10.0/Lolcode.CodeAnalysis.dll /tmp/lolcode-pkg/tools/net10.0/
   cp src/Lolcode.Build/bin/Debug/net10.0/Lolcode.Runtime.dll /tmp/lolcode-pkg/tools/net10.0/

   # Create .nuspec
   cat > /tmp/lolcode-pkg/Lolcode.NET.Sdk.nuspec << 'EOF'
   <?xml version="1.0" encoding="utf-8"?>
   <package xmlns="http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd">
     <metadata>
       <id>Lolcode.NET.Sdk</id>
       <version>0.1.0-local</version>
       <description>MSBuild SDK for LOLCODE</description>
       <authors>dotnet-languages</authors>
       <packageTypes><packageType name="MSBuildSdk" /></packageTypes>
     </metadata>
   </package>
   EOF

   # Pack and place in feed
   cd /tmp/lolcode-pkg && zip -r /tmp/lolcode-feed/lolcode.net.sdk.0.1.0-local.nupkg Sdk/ tools/ Lolcode.NET.Sdk.nuspec
   ```

3. **Create a test project:**
   ```bash
   mkdir -p /tmp/test-lolcode && cd /tmp/test-lolcode

   # Create global.json to reference SDK
   echo '{"msbuild-sdks":{"Lolcode.NET.Sdk":"0.1.0-local"}}' > global.json

   # Create NuGet.config pointing to local feed
   cat > NuGet.config << 'EOF'
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <packageSources>
       <clear />
       <add key="local" value="/tmp/lolcode-feed" />
       <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
     </packageSources>
   </configuration>
   EOF

   # Create .lolproj
   cat > hello.lolproj << 'EOF'
   <Project Sdk="Lolcode.NET.Sdk">
     <PropertyGroup>
       <OutputType>Exe</OutputType>
       <TargetFramework>net10.0</TargetFramework>
     </PropertyGroup>
   </Project>
   EOF

   # Create LOLCODE source
   cat > hello.lol << 'EOF'
   HAI 1.2
     VISIBLE "HAI WORLD!"
   KTHXBYE
   EOF

   # Build and run!
   dotnet build
   dotnet run
   ```

4. **Iterate on changes:**
   ```bash
   # After making changes to the compiler or SDK:
   dotnet build dotnet-lolcode.slnx

   # Rebuild the NuGet package (repeat step 2)
   # Clear NuGet cache to pick up changes:
   rm -rf ~/.nuget/packages/lolcode.net.sdk

   # Rebuild test project:
   cd /tmp/test-lolcode && rm -rf obj bin && dotnet build
   ```

## What Works

- `dotnet build` — Compiles `.lol` files to .NET assemblies
- `dotnet run` — Compiles and executes the program
- `dotnet publish` — Creates deployable output with runtime
- `dotnet clean` — Cleans build artifacts
- Incremental builds — Skips compilation when nothing changed
- `.lolproj` files with `<Project Sdk="Lolcode.NET.Sdk">`