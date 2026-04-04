#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
APPHOST_PROJECT="Picea.Abies.Conduit.AppHost/Picea.Abies.Conduit.AppHost.csproj"
APPHOST_LOG="/tmp/abies-conduit-apphost-clean.log"
APPHOST_PID_FILE="/tmp/abies-conduit-apphost-clean.pid"

CONDUIT_PATTERN='Picea\.Abies\.Conduit\.(AppHost|Api|Server|Wasm\.Host)|Conduit\.(AppHost|Api|Server|Wasm\.Host)\.csproj'

cleanup_processes() {
  local pids
  pids="$(pgrep -f "$CONDUIT_PATTERN" || true)"
  if [[ -n "${pids:-}" ]]; then
    echo "Stopping existing Conduit processes..."
    while IFS= read -r pid; do
      [[ -n "$pid" ]] && kill "$pid" 2>/dev/null || true
    done <<< "$pids"

    sleep 1

    pids="$(pgrep -f "$CONDUIT_PATTERN" || true)"
    if [[ -n "${pids:-}" ]]; then
      echo "Force-stopping remaining Conduit processes..."
      while IFS= read -r pid; do
        [[ -n "$pid" ]] && kill -9 "$pid" 2>/dev/null || true
      done <<< "$pids"
    fi
  fi
}

wait_for_dashboard() {
  local attempts=40
  local i
  for ((i=1; i<=attempts; i++)); do
    if lsof -nP -iTCP:22100 -sTCP:LISTEN >/dev/null 2>&1; then
      return 0
    fi
    sleep 0.5
  done

  echo "AppHost did not bind dashboard port 22100 in time."
  echo "Log tail:"
  tail -n 80 "$APPHOST_LOG" || true
  return 1
}

trigger_resource_startup() {
  # Touch the dashboard once so resource orchestration is initialized.
  curl -ks https://localhost:17195/ >/dev/null 2>&1 || true
}

resolve_frontend_url() {
  local ports port root_code api_code body candidate

  candidate=""
  for _ in $(seq 1 160); do
    ports="$(lsof -nP -iTCP -sTCP:LISTEN 2>/dev/null | rg '(Picea\.Abi|dotnet)' | awk '{print $9}' | sed -E 's/.*:([0-9]+)$/\1/' | sort -n | uniq)"

    while IFS= read -r port; do
      [[ -z "$port" ]] && continue
      root_code="$(curl -sk -o /tmp/abies-root-probe.out -w '%{http_code}' "http://localhost:${port}/" || true)"
      api_code="$(curl -sk -o /tmp/abies-api-probe.out -w '%{http_code}' "http://localhost:${port}/api/tags" || true)"
      if [[ -f /tmp/abies-root-probe.out ]]; then
        body="$(head -c 220 /tmp/abies-root-probe.out)"
      else
        body=""
      fi

      if [[ "$root_code" == "200" ]] && echo "$body" | rg -qi '<title>Conduit'; then
        candidate="http://localhost:${port}"
        if [[ "$api_code" == "200" || "$api_code" == "204" ]]; then
          echo "$candidate"
          return 0
        fi
      fi
    done <<< "$ports"

    if [[ -n "$candidate" ]]; then
      echo "$candidate"
      return 0
    fi

    sleep 0.5
  done

  echo ""
}

main() {
  cd "$ROOT_DIR"

  cleanup_processes

  echo "Starting AppHost..."
  nohup dotnet run --project "$APPHOST_PROJECT" --no-build > "$APPHOST_LOG" 2>&1 &
  echo $! > "$APPHOST_PID_FILE"

  wait_for_dashboard
  trigger_resource_startup

  local frontend_url
  frontend_url="$(resolve_frontend_url)"

  echo
  echo "Conduit stack started."
  echo "Dashboard: https://localhost:17195"
  if [[ -n "$frontend_url" ]]; then
    echo "Frontend:  ${frontend_url}"
    echo "OTLP:      ${frontend_url}/otlp/v1/traces"
  else
    echo "Frontend:  (not detected yet; check $APPHOST_LOG)"
  fi
  echo "Log:       $APPHOST_LOG"
}

main "$@"
