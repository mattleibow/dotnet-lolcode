# Project-Based Hello World

This sample demonstrates the optional `.lolproj` workflow. Most samples in
this repository are file-based apps.

```bash
dotnet build dotnet-lolcode.slnx
dotnet run --project samples/project-based/hello-world/hello-world.lolproj
```
# Publishing

This project supports normal SDK publishing. For example, on macOS Arm64:

```console
dotnet publish hello-world.lolproj -c Release -r osx-arm64 -p:PublishAot=true --self-contained true
```

See [NativeAOT publishing](../../../docs/NATIVE_AOT.md) for static-import
requirements and the distinction between managed single-file and NativeAOT
output.
