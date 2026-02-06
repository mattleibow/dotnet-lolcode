# Hello World — Lolcode.NET.Sdk Sample

A minimal LOLCODE project using the `Lolcode.NET.Sdk` MSBuild SDK.

## Prerequisites

Build the compiler from the repository root:

```bash
dotnet build dotnet-lolcode.slnx
```

## Usage

```bash
cd samples/sdk-hello-world
dotnet build
dotnet run
```

## Project Structure

| File | Purpose |
|------|---------|
| `HelloWorld.lolproj` | Project file — imports LOLCODE SDK from source tree |
| `Program.lol` | LOLCODE source code |
