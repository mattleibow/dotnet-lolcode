# Hello World — Lolcode.NET.Sdk Sample

A minimal LOLCODE project using the `Lolcode.NET.Sdk` MSBuild SDK.

## Prerequisites

Build and package the SDK from the repository root:

```bash
# From the repository root:
dotnet build dotnet-lolcode.slnx
./pack-local.sh
```

## Usage

```bash
dotnet build
dotnet run
```

## Project Structure

| File | Purpose |
|------|---------|
| `HelloWorld.lolproj` | Project file — references `Lolcode.NET.Sdk` |
| `Program.lol` | LOLCODE source code |
| `global.json` | Pins the SDK version for NuGet resolution |
| `NuGet.config` | Points to the local NuGet feed |
| `sdk-hello-world.slnx` | Solution file for IDE support |
