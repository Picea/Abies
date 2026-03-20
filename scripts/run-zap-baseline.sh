#!/usr/bin/env bash
set -euo pipefail

target_url="${1:-http://127.0.0.1:5179}"
output_dir="${2:-zap-results}"

mkdir -p "$output_dir"

require_cmd() {
  local cmd="$1"
  if ! command -v "$cmd" >/dev/null 2>&1; then
    echo "$cmd is required but was not found in PATH"
    exit 1
  fi
}

require_cmd docker
require_cmd curl

if ! docker info >/dev/null 2>&1; then
  echo "Docker daemon is not available. Start Docker Desktop and retry."
  exit 1
fi

if ! curl -sS --max-time 5 "$target_url" >/dev/null; then
  echo "Target URL is not reachable: $target_url"
  echo "Ensure the target app and dependencies are running before DAST scans."
  exit 1
fi

echo "Running ZAP baseline against $target_url"
# Note: pass relative paths to ZAP — the container CWD is /zap/wrk (mounted from pwd).
# Absolute paths containing /zap/wrk get doubled to /zap/wrk/zap/wrk/... by ZAP internally.
docker run --rm --network host \
  --user 0:0 \
  -v "$(pwd):/zap/wrk:rw" \
  ghcr.io/zaproxy/zaproxy:stable \
  zap-baseline.py \
    -t "$target_url" \
    -m 5 \
    -J "$output_dir/zap-report.json" \
    -r "$output_dir/zap-report.html" \
    -w "$output_dir/zap-report.md" \
    -x "$output_dir/zap-report.xml" \
    -I

if ! command -v jq >/dev/null 2>&1; then
  echo "jq not found; skipping high-risk summary"
  exit 0
fi

if [ ! -f "$output_dir/zap-report.json" ]; then
  echo "ZAP report was not generated at $output_dir/zap-report.json"
  echo "The target may be unreachable or returned an unexpected status for baseline crawling."
  exit 1
fi

high_count=$(jq '[.site[].alerts[] | select((.riskcode | tonumber) >= 3)] | length' "$output_dir/zap-report.json")
echo "ZAP high-risk alerts: $high_count"

if [ "$high_count" -gt 0 ]; then
  echo "High-risk DAST findings detected."
  exit 1
fi

echo "No high-risk DAST findings detected."
