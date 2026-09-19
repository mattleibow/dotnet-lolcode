#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 1 || $# -gt 2 ]]; then
  printf 'Usage: %s <pull-request-number> [port]\n' "$0" >&2
  exit 2
fi

pr_number="$1"
port="${2:-}"
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

command -v gh >/dev/null
command -v python3 >/dev/null

if [[ -z "$port" ]]; then
  port="$(
    python3 -c 'import socket; s = socket.socket(); s.bind(("127.0.0.1", 0)); print(s.getsockname()[1]); s.close()'
  )"
fi

head_sha="$(gh pr view "$pr_number" --json headRefOid --jq .headRefOid)"
run_id="$(
  gh run list \
    --workflow pages.yml \
    --commit "$head_sha" \
    --event pull_request \
    --status completed \
    --limit 20 \
    --json databaseId,conclusion \
    --jq '[.[] | select(.conclusion == "success")][0].databaseId'
)"

if [[ -z "$run_id" || "$run_id" == "null" ]]; then
  printf 'No successful Pages workflow run found for PR #%s at %s.\n' \
    "$pr_number" "$head_sha" >&2
  printf 'Check it with: gh pr checks %s\n' "$pr_number" >&2
  exit 1
fi

preview_dir="$(mktemp -d "${TMPDIR:-/tmp}/dotnet-lolcode-pr-${pr_number}.XXXXXX")"
artifact_dir="$preview_dir/artifact"
server_root="$preview_dir/server"
mkdir -p "$artifact_dir" "$server_root"

gh run download "$run_id" --name dotnet-lolcode-pages --dir "$artifact_dir"
ln -s "$artifact_dir" "$server_root/dotnet-lolcode"

python3 -m http.server "$port" --bind 127.0.0.1 --directory "$server_root" \
  >"$preview_dir/server.log" 2>&1 &
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
    cat "$preview_dir/server.log" >&2
    exit 1
  fi
  sleep 1
done

curl --fail --silent "$base_url" >/dev/null
LOLCODE_SITE_URL="$base_url" \
LOLCODE_SCREENSHOT_DIR="$preview_dir/screenshots" \
  dotnet test tests/Lolcode.Docs.Tests/Lolcode.Docs.Tests.csproj \
  --verbosity normal

printf '\nPR #%s Pages preview is ready:\n%s\n' "$pr_number" "$base_url"
printf 'Artifact: %s\nScreenshots: %s\n' "$artifact_dir" "$preview_dir/screenshots"
printf 'Press Ctrl+C to stop the server.\n'
wait "$server_pid"
