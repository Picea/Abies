#!/usr/bin/env bash
set -euo pipefail

target_url="${1:-http://127.0.0.1:5179}"
output_dir="${2:-zap-results-auth}"
policy_file="${3:-.zap/apphost-auth-policy.conf}"
targets_file="${4:-.zap/apphost-auth-targets.txt}"

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
require_cmd jq

if ! docker info >/dev/null 2>&1; then
  echo "Docker daemon is not available. Start Docker Desktop and retry."
  exit 1
fi

if ! curl -fsS --max-time 5 "$target_url/api/tags" >/dev/null 2>&1; then
  echo "Target API is not ready at $target_url/api/tags"
  echo "Ensure API dependencies (for example PostgreSQL) are running and retry."
  exit 1
fi

if [ ! -f "$policy_file" ]; then
  echo "Policy file not found: $policy_file"
  exit 1
fi

if [ ! -f "$targets_file" ]; then
  echo "Targets file not found: $targets_file"
  exit 1
fi

timestamp="$(date +%s)"
username="zap_user_${timestamp}"
email="zap_${timestamp}@example.com"
password="P@ssword123!"

register_payload=$(cat <<EOF
{"user":{"username":"$username","email":"$email","password":"$password"}}
EOF
)

login_payload=$(cat <<EOF
{"user":{"email":"$email","password":"$password"}}
EOF
)

curl -fsS \
  -H "Content-Type: application/json" \
  -d "$register_payload" \
  "$target_url/api/users" >/dev/null

login_response=$(curl -fsS \
  -H "Content-Type: application/json" \
  -d "$login_payload" \
  "$target_url/api/users/login")

token=$(echo "$login_response" | jq -r '.user.token')
if [ -z "$token" ] || [ "$token" = "null" ]; then
  echo "Unable to obtain JWT token from login response"
  exit 1
fi

authenticated_endpoints=()
while IFS= read -r raw_line || [ -n "$raw_line" ]; do
  line="$(echo "$raw_line" | sed -e 's/^\s*//' -e 's/\s*$//')"
  if [ -z "$line" ] || [[ "$line" == \#* ]]; then
    continue
  fi

  if [[ "$line" == http://* ]] || [[ "$line" == https://* ]]; then
    authenticated_endpoints+=("$line")
  else
    authenticated_endpoints+=("$target_url$line")
  fi
done < "$targets_file"

if [ "${#authenticated_endpoints[@]}" -eq 0 ]; then
  echo "No authenticated endpoints found in $targets_file"
  exit 1
fi

total_high=0
for endpoint in "${authenticated_endpoints[@]}"; do
  name=$(echo "$endpoint" | sed 's#https\?://##' | tr '/:' '_')
  json_report="/zap/wrk/$output_dir/${name}.json"
  html_report="/zap/wrk/$output_dir/${name}.html"
  md_report="/zap/wrk/$output_dir/${name}.md"
  xml_report="/zap/wrk/$output_dir/${name}.xml"

  echo "Running authenticated ZAP baseline against $endpoint"
  docker run --rm --network host \
    -v "$(pwd):/zap/wrk:rw" \
    ghcr.io/zaproxy/zaproxy:stable \
    zap-baseline.py \
      -t "$endpoint" \
      -m 3 \
      -c "/zap/wrk/$policy_file" \
      -J "$json_report" \
      -r "$html_report" \
      -w "$md_report" \
      -x "$xml_report" \
      -I \
      -z "-config replacer.full_list(0).description=auth -config replacer.full_list(0).enabled=true -config replacer.full_list(0).matchtype=REQ_HEADER -config replacer.full_list(0).matchstr=Authorization -config replacer.full_list(0).replacement=Token\ $token"

  high_count=$(jq '[.site[].alerts[] | select((.riskcode | tonumber) >= 3)] | length' "$output_dir/${name}.json")
  total_high=$((total_high + high_count))
done

echo "Authenticated ZAP high-risk alerts: $total_high"
if [ "$total_high" -gt 0 ]; then
  echo "High-risk authenticated DAST findings detected."
  exit 1
fi

echo "No high-risk authenticated DAST findings detected."
