#!/usr/bin/env bash
set -euo pipefail

repo_root="$(git rev-parse --show-toplevel)"
cd "$repo_root"

git config core.hooksPath .githooks
chmod +x .githooks/pre-commit

echo "Configured git hooks path to .githooks"
echo "Pre-commit hook enabled: .githooks/pre-commit"
