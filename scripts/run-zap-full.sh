#!/usr/bin/env bash
# run-zap-full.sh — Nightly full active ZAP scan against Conduit frontend + API.
#
# Runs three sequential passes:
#   0. Frontend pass (PRIMARY): active-scan the WASM host (HTML, headers, JS surface).
#   1. Unauthenticated API pass: active-scan public API endpoints.
#   2. Authenticated API pass: active-scan protected endpoints with JWT injected.
#
# Uses zap-full-scan.py (active scanner) rather than zap-baseline.py (passive only).
# Does NOT use -I (ignore errors flag) — nightly scan must hard-fail on HIGH findings.
#
# Usage: bash scripts/run-zap-full.sh [api-base-url] [frontend-url]
#   Defaults: api=http://127.0.0.1:5179  frontend=http://127.0.0.1:5200

set -euo pipefail

API_BASE="${1:-http://127.0.0.1:5179}"
FRONTEND_URL="${2:-http://127.0.0.1:5200}"
ZAP_IMAGE="ghcr.io/zaproxy/zaproxy:stable"
REPORT_DIR="${GITHUB_WORKSPACE:-$(pwd)}/zap-nightly-reports"
POLICY_FILE=".zap/full-scan-policy.conf"
AUTH_TARGETS=".zap/full-scan-targets.txt"
SPIDER_MINUTES=10
# Username must match Conduit's domain rule: 1-20 chars, [a-zA-Z0-9_-].
# Use a deterministic short prefix plus a 12-digit timestamp slice for uniqueness.
EPHEMERAL_USER="zap-$(date +%s%N | cut -c 8-19)"
EPHEMERAL_EMAIL="${EPHEMERAL_USER}@zap.invalid"
EPHEMERAL_PASS="ZapN!ghtlyP@ss$(date +%s)"

mkdir -p "$REPORT_DIR"
# Ensure the mounted report directory is writable by the ZAP container user.
chmod a+rwx "$REPORT_DIR"

echo "================================================================"
echo "Conduit — ZAP Full Active Scan (frontend primary)"
echo "Frontend  : $FRONTEND_URL"
echo "API base  : $API_BASE"
echo "Reports   : $REPORT_DIR"
echo "Spider    : ${SPIDER_MINUTES} min"
echo "================================================================"

# ------------------------------------------------------------------ #
#  Shared helpers                                                      #
# ------------------------------------------------------------------ #

docker_zap() {
  if [ -z "${REPORT_DIR:-}" ]; then
    echo "docker_zap requires REPORT_DIR to be set" >&2
    return 1
  fi

  local workspace_dir="${GITHUB_WORKSPACE:-$(pwd)}"
  local docker_user="$(id -u):$(id -g)"
  local report_dir="$REPORT_DIR"
  # Scan commands write reports under zap-nightly-reports/* inside /zap/wrk.
  local container_report_dir="/zap/wrk/zap-nightly-reports"
  if [ "${GITHUB_ACTIONS:-false}" = "true" ]; then
    # In GitHub-hosted CI, the host UID can be unmapped inside the ZAP image.
    # Falling back to root avoids intermittent "Failed to start ZAP" startup errors.
    docker_user="0:0"
  fi

  docker run --rm \
    --user "$docker_user" \
    --network host \
    -v "${workspace_dir}:/zap/wrk/:ro" \
    -v "${report_dir}:${container_report_dir}:rw" \
    "$ZAP_IMAGE" \
    "$@"
}

check_high() {
  local report_file="$1"
  local phase="$2"

  if [ ! -f "$report_file" ]; then
    echo "⚠  No JSON report found for phase '$phase'. Treating as failure."
    return 1
  fi

  high_count=$(jq '[.site[].alerts[] | select(.riskcode == "3")] | length' "$report_file" 2>/dev/null || echo "0")
  echo "Phase '$phase': HIGH risk alerts = $high_count"

  if [ "$high_count" -gt 0 ]; then
    echo "❌ FAIL — $high_count HIGH-risk finding(s) in phase '$phase'."
    return 1
  fi

  echo "✅ PASS — no HIGH-risk findings in phase '$phase'."
  return 0
}

# ------------------------------------------------------------------ #
#  Phase 0 — Frontend active scan (PRIMARY)                           #
#  Scans the WASM host for security headers, XSS, clickjacking, CSP.  #
# ------------------------------------------------------------------ #

echo ""
echo "---- Phase 0: Frontend full scan (PRIMARY) --------------------"

docker_zap zap-full-scan.py \
  -t "$FRONTEND_URL" \
  -m "$SPIDER_MINUTES" \
  -c "/zap/wrk/${POLICY_FILE}" \
  -J "zap-nightly-reports/phase0-frontend.json" \
  -r "zap-nightly-reports/phase0-frontend.html" \
  -w "zap-nightly-reports/phase0-frontend.md" \
  -x "zap-nightly-reports/phase0-frontend.xml" \
  -d \
  || true

check_high "$REPORT_DIR/phase0-frontend.json" "frontend"
PHASE0_STATUS=$?

# ------------------------------------------------------------------ #
#  Phase 1 — Unauthenticated active scan (public API surface)         #
# ------------------------------------------------------------------ #

echo ""
echo "---- Phase 1: Unauthenticated API full scan -------------------"

docker_zap zap-full-scan.py \
  -t "${API_BASE}/api/tags" \
  -m "$SPIDER_MINUTES" \
  -c "/zap/wrk/${POLICY_FILE}" \
  -J "zap-nightly-reports/phase1-unauth.json" \
  -r "zap-nightly-reports/phase1-unauth.html" \
  -w "zap-nightly-reports/phase1-unauth.md" \
  -x "zap-nightly-reports/phase1-unauth.xml" \
  -d \
  || PHASE1_EXIT=$?

check_high "$REPORT_DIR/phase1-unauth.json" "unauth"
PHASE1_STATUS=$?

# ------------------------------------------------------------------ #
#  Register ephemeral user and obtain JWT                             #
# ------------------------------------------------------------------ #

echo ""
echo "---- Auth setup: registering ephemeral user -------------------"

REGISTER_RESPONSE=$(curl -fs -X POST "${API_BASE}/api/users" \
  -H "Content-Type: application/json" \
  -d "{\"user\":{\"username\":\"${EPHEMERAL_USER}\",\"email\":\"${EPHEMERAL_EMAIL}\",\"password\":\"${EPHEMERAL_PASS}\"}}" \
  || true)

if [ -z "$REGISTER_RESPONSE" ]; then
  echo "❌ Failed to register ephemeral user. Cannot run authenticated pass."
  exit 1
fi

echo "Registered: $EPHEMERAL_USER"

LOGIN_RESPONSE=$(curl -fs -X POST "${API_BASE}/api/users/login" \
  -H "Content-Type: application/json" \
  -d "{\"user\":{\"email\":\"${EPHEMERAL_EMAIL}\",\"password\":\"${EPHEMERAL_PASS}\"}}")

TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.user.token // empty')

if [ -z "$TOKEN" ]; then
  echo "❌ Failed to obtain JWT. Login response: $LOGIN_RESPONSE"
  exit 1
fi

echo "JWT obtained (length: ${#TOKEN})"

# ------------------------------------------------------------------ #
#  Seed a test article so slug-based endpoints exist                   #
# ------------------------------------------------------------------ #

echo ""
echo "---- Seeding test article -------------------------------------"

ARTICLE_RESPONSE=$(curl -fs -X POST "${API_BASE}/api/articles" \
  -H "Content-Type: application/json" \
  -H "Authorization: Token $TOKEN" \
  -d '{"article":{"title":"ZAP Nightly Test Article","description":"Created by ZAP nightly scan","body":"Test body for scan","tagList":["zap","nightly"]}}' \
  || true)

ARTICLE_SLUG=$(echo "$ARTICLE_RESPONSE" | jq -r '.article.slug // "zap-nightly-test-article"')
echo "Article slug: $ARTICLE_SLUG"

# ------------------------------------------------------------------ #
#  Phase 2 — Authenticated active scan (protected surface)            #
# ------------------------------------------------------------------ #

echo ""
echo "---- Phase 2: Authenticated full scan -------------------------"

PHASE2_STATUS=0
total_high=0

while IFS= read -r endpoint || [ -n "$endpoint" ]; do
  # Skip comments and blank lines
  [[ "$endpoint" =~ ^#.*$ || -z "$endpoint" ]] && continue

  # Substitute placeholder slugs with the seeded article slug
  target_path="${endpoint/zap-nightly-article/$ARTICLE_SLUG}"
  target_path="${target_path/zap-nightly-user/$EPHEMERAL_USER}"
  target_url="${API_BASE}${target_path}"

  safe_name=$(echo "$target_path" | tr '/ ' '__')
  report_base="zap-nightly-reports/phase2-auth${safe_name}"

  echo "  Scanning: $target_url"

  docker_zap zap-full-scan.py \
    -t "$target_url" \
    -m "$SPIDER_MINUTES" \
    -c "/zap/wrk/${POLICY_FILE}" \
    -J "zap-nightly-reports/phase2-auth${safe_name}.json" \
    -r "zap-nightly-reports/phase2-auth${safe_name}.html" \
    -w "zap-nightly-reports/phase2-auth${safe_name}.md" \
    -x "zap-nightly-reports/phase2-auth${safe_name}.xml" \
    -z "-config replacer.full_list(0).description=AuthHeader \
        -config replacer.full_list(0).enabled=true \
        -config replacer.full_list(0).matchtype=REQ_HEADER \
        -config replacer.full_list(0).matchstr=Authorization \
        -config replacer.full_list(0).replacement=Token\\ $TOKEN" \
    -d \
    || true

  endpoint_high=$(jq '[.site[].alerts[] | select(.riskcode == "3")] | length' \
    "${REPORT_DIR}/phase2-auth${safe_name}.json" 2>/dev/null || echo "0")
  echo "    HIGH alerts: $endpoint_high"
  total_high=$((total_high + endpoint_high))

done < "$AUTH_TARGETS"

if [ "$total_high" -gt 0 ]; then
  echo "❌ FAIL — $total_high HIGH-risk finding(s) across authenticated endpoints."
  PHASE2_STATUS=1
else
  echo "✅ PASS — no HIGH-risk findings in authenticated phase."
fi

# ------------------------------------------------------------------ #
#  Summary                                                             #
# ------------------------------------------------------------------ #

echo ""
echo "================================================================"
echo "Reports: $REPORT_DIR"
echo "  phase0-frontend.*  : frontend (WASM host) active scan  [PRIMARY]"
echo "  phase1-unauth.*    : unauthenticated API active scan"
echo "  phase2-auth__*.*   : authenticated API active scan per endpoint"
echo "================================================================"

if [ "${PHASE0_STATUS:-0}" -ne 0 ] || [ "${PHASE1_STATUS:-0}" -ne 0 ] || [ "${PHASE2_STATUS:-0}" -ne 0 ]; then
  echo "❌ ZAP Full Scan FAILED (see HIGH-risk findings above)."
  exit 1
fi

echo "✅ ZAP Full Scan PASSED — no HIGH-risk findings."
