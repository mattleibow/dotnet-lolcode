# File-Based Hello World

A minimal LOLCODE program that runs without a `.lolproj` file, using .NET 10's file-based app support.

## Usage

```bash
dotnet run --file hello.lol
# Or:
dotnet hello.lol
```

## How It Works

The `#:sdk Lolcode.NET.Sdk` directive at the top tells the .NET SDK to use the LOLCODE compiler. No project file, no `global.json`, no `NuGet.config` needed (when the SDK is installed from NuGet).

For local development, you need a `NuGet.config` pointing to the local feed:
```bash
cd /path/to/dotnet-lolcode
./pack-local.sh
cd samples/file-based-hello
dotnet run --file hello.lol
```
