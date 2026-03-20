#!/usr/bin/env bash
set -euo pipefail

repo_root="$(git rev-parse --show-toplevel)"
cd "$repo_root"

output_dir="${1:-trivy-results}"
mkdir -p "$output_dir"

if ! command -v trivy >/dev/null 2>&1; then
  cat <<'EOF'
trivy is not installed.
Install with one of:
  brew install trivy
  docker run --rm aquasec/trivy:latest --help
EOF
  exit 1
fi

echo "Running Trivy filesystem scan (HIGH/CRITICAL gate)..."
trivy fs \
  --config trivy.yaml \
  --output "$output_dir/trivy-fs.txt" \
  .

echo "Generating SARIF report..."
trivy fs \
  --config trivy.yaml \
  --format sarif \
  --exit-code 0 \
  --output "$output_dir/trivy-fs.sarif" \
  .

echo "Running Dockerfile misconfiguration scan..."
trivy config \
  --severity HIGH,CRITICAL \
  --exit-code 1 \
  --output "$output_dir/trivy-dockerfile.txt" \
  Picea.Abies.Conduit.AppHost/Dockerfile

echo "Trivy baseline completed."
