#!/usr/bin/env bash
set -euo pipefail

target_url="${1:-http://127.0.0.1:5179}"
output_dir="${2:-zap-results}"

mkdir -p "$output_dir"

echo "Running ZAP baseline against $target_url"
docker run --rm --network host \
  -v "$(pwd):/zap/wrk:rw" \
  ghcr.io/zaproxy/zaproxy:stable \
  zap-baseline.py \
    -t "$target_url" \
    -m 5 \
    -J "/zap/wrk/$output_dir/zap-report.json" \
    -r "/zap/wrk/$output_dir/zap-report.html" \
    -w "/zap/wrk/$output_dir/zap-report.md" \
    -x "/zap/wrk/$output_dir/zap-report.xml" \
    -I

if ! command -v jq >/dev/null 2>&1; then
  echo "jq not found; skipping high-risk summary"
  exit 0
fi

high_count=$(jq '[.site[].alerts[] | select((.riskcode | tonumber) >= 3)] | length' "$output_dir/zap-report.json")
echo "ZAP high-risk alerts: $high_count"

if [ "$high_count" -gt 0 ]; then
  echo "High-risk DAST findings detected."
  exit 1
fi

echo "No high-risk DAST findings detected."
