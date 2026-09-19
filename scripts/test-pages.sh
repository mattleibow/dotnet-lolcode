#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
site="${1:-$repo_root/docs/_site}"
port="${2:-}"

if [[ -z "$port" ]]; then
  port="$(
    python3 -c 'import socket; s = socket.socket(); s.bind(("127.0.0.1", 0)); print(s.getsockname()[1]); s.close()'
  )"
fi

if [[ ! -f "$site/index.html" ]]; then
  printf 'Pages artifact not found at %s. Run scripts/build-pages.sh first.\n' "$site" >&2
  exit 1
fi

preview_root="$(mktemp -d "${TMPDIR:-/tmp}/dotnet-lolcode-preview.XXXXXX")"
mkdir -p "$preview_root"
ln -s "$(cd "$site" && pwd)" "$preview_root/dotnet-lolcode"

python3 -m http.server "$port" --bind 127.0.0.1 --directory "$preview_root" \
  >"$preview_root/server.log" 2>&1 &
server_pid=$!

cleanup() {
  kill "$server_pid" 2>/dev/null || true
}
trap cleanup EXIT INT TERM

base_url="http://127.0.0.1:$port/dotnet-lolcode/"
for _ in {1..30}; do
  if curl --fail --silent "$base_url" >/dev/null; then
    break
  fi
  if ! kill -0 "$server_pid" 2>/dev/null; then
    cat "$preview_root/server.log" >&2
    exit 1
  fi
  sleep 1
done

curl --fail --silent "$base_url" >/dev/null
LOLCODE_SITE_URL="$base_url" \
LOLCODE_SCREENSHOT_DIR="$repo_root/artifacts/playwright" \
  dotnet test "$repo_root/tests/Lolcode.Docs.Tests/Lolcode.Docs.Tests.csproj" \
  --verbosity normal

printf 'Playwright screenshots: %s\n' "$repo_root/artifacts/playwright"
