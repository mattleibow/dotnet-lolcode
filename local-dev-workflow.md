# Local SDK Development Workflow

1. **Build the SDK components:**
   ```bash
   dotnet build src/Lolcode.CodeAnalysis
   dotnet build src/Lolcode.Runtime
   dotnet build src/Lolcode.Build  # New MSBuild task project
   ```

2. **Pack the SDK locally:**
   ```bash
   dotnet pack src/Lolcode.NET.Sdk --configuration Debug --output ./local-packages
   ```

3. **Test with a local NuGet source:**
   ```bash
   # Add local package source
   dotnet nuget add source ./local-packages --name local-dev
   
   # Create test project
   mkdir test-sdk && cd test-sdk
   echo '{"msbuild-sdks":{"Lolcode.NET.Sdk":"1.0.0-dev"}}' > global.json
   dotnet new console --force
   # Edit .csproj to use Sdk="Lolcode.NET.Sdk"
   ```

4. **Iterate:**
   ```bash
   # Make changes, rebuild, repack
   dotnet pack src/Lolcode.NET.Sdk --configuration Debug --output ./local-packages
   
   # Clear NuGet cache to pick up changes
   dotnet nuget locals all --clear
   
   # Test again
   cd test-sdk && dotnet build
   ```