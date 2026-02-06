#!/bin/bash
# Builds the Lolcode.NET.Sdk NuGet package into local-feed/ for sample projects.
set -euo pipefail
REPO_ROOT="$(cd "$(dirname "$0")" && pwd)"

echo "Building solution..."
dotnet build "$REPO_ROOT/dotnet-lolcode.slnx" --verbosity quiet

echo "Packaging Lolcode.NET.Sdk..."
rm -rf "$REPO_ROOT/local-feed"
mkdir -p "$REPO_ROOT/local-feed"

PKG_DIR=$(mktemp -d)
mkdir -p "$PKG_DIR/Sdk" "$PKG_DIR/tools/net10.0"
cp "$REPO_ROOT/Sdk/Sdk.props" "$REPO_ROOT/Sdk/Sdk.targets" "$PKG_DIR/Sdk/"
cp "$REPO_ROOT/src/Lolcode.Build/bin/Debug/net10.0/Lolcode.Build.dll" \
   "$REPO_ROOT/src/Lolcode.Build/bin/Debug/net10.0/Lolcode.Build.deps.json" \
   "$REPO_ROOT/src/Lolcode.Build/bin/Debug/net10.0/Lolcode.CodeAnalysis.dll" \
   "$REPO_ROOT/src/Lolcode.Build/bin/Debug/net10.0/Lolcode.Runtime.dll" \
   "$PKG_DIR/tools/net10.0/"
cat > "$PKG_DIR/Lolcode.NET.Sdk.nuspec" << 'EOF'
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
(cd "$PKG_DIR" && zip -qr "$REPO_ROOT/local-feed/lolcode.net.sdk.0.1.0-local.nupkg" Sdk/ tools/ Lolcode.NET.Sdk.nuspec)
rm -rf "$PKG_DIR"

# Clear cached version so MSBuild picks up the new one
rm -rf ~/.nuget/packages/lolcode.net.sdk

echo "Done! Package at: local-feed/lolcode.net.sdk.0.1.0-local.nupkg"
echo ""
echo "Test it:"
echo "  cd samples/sdk-hello-world && dotnet run"
