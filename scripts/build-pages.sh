#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

site="docs/_site"
playground_output="artifacts/lolcode-web"
playground="$playground_output/wwwroot"

dotnet tool restore
rm -rf docs/_site artifacts/lolcode-web
dotnet tool run docfx docs/public/docfx.json --warningsAsErrors
dotnet publish src/Lolcode.Web/Lolcode.Web.csproj \
  --configuration Release \
  --output "$playground_output"

grep -Fq '<base href="/dotnet-lolcode/playground/" />' "$playground/index.html"
cp -R docs/public/landing/. "$site/"
mkdir -p "$site/playground"
cp -R "$playground"/. "$site/playground/"
touch "$site/.nojekyll"

test -f "$site/index.html"
test -f "$site/main.css"
test -f "$site/404.html"
test -f "$site/docs/index.html"
test -f "$site/docs/api/Lolcode.html"
test -f "$site/playground/_framework/blazor.webassembly.js"
test ! -e "$site/docs/dev"

printf 'Pages artifact built at %s\n' "$site"
