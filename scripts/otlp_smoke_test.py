#!/usr/bin/env python3

import os
import sys
import time
import urllib.error
import urllib.request


def main() -> int:
    api_proxy_url = os.environ.get("ABIES_OTLP_PROXY_URL", "http://localhost:5179/otlp/v1/traces")
    aspire_otlp_url = os.environ.get("ABIES_ASPIRE_OTLP_URL", "https://localhost:21203/v1/traces")

    # Minimal OTLP payload: not a valid ExportTraceServiceRequest, but good enough to
    # validate proxy routing/auth/CORS doesn't 403/415 before we send real data.
    payload = bytes([0x0A, 0x00])

    def is_reachable(url: str) -> bool:
        # OTLP endpoints generally don't support GET; we only need to know the TCP listener is up.
        # We'll try a POST with a tiny payload and treat any HTTP response as "reachable".
        req = urllib.request.Request(
            url,
            data=payload,
            headers={"Content-Type": "application/x-protobuf"},
            method="POST",
        )
        try:
            urllib.request.urlopen(req, timeout=2)
            return True
        except urllib.error.HTTPError:
            return True
        except Exception:
            return False

    # Wait briefly for the API to be up (useful when launched by a task)
    deadline = time.time() + float(os.environ.get("ABIES_OTLP_WAIT_SECONDS", "8"))
    while time.time() < deadline and not is_reachable(api_proxy_url):
        time.sleep(0.25)

    if not is_reachable(api_proxy_url):
        print("target", api_proxy_url)
        print("status", "unreachable")
        print("note", "The API proxy endpoint isn't reachable. Ensure Abies.Conduit.Api is running on port 5179.")
        return 2

    req = urllib.request.Request(
        api_proxy_url,
        data=payload,
        headers={"Content-Type": "application/x-protobuf"},
        method="POST",
    )

    try:
        resp = urllib.request.urlopen(req, timeout=5)
        body = resp.read()
        print("target", api_proxy_url)
        print("status", resp.status)
        print("content-type", resp.headers.get("content-type"))
        print("x-otlp-proxy-has-key", resp.headers.get("x-otlp-proxy-has-key"))
        print("x-otlp-proxy-key-sha256", resp.headers.get("x-otlp-proxy-key-sha256"))
        print("body_len", len(body))
        print(
            "note",
            "If status is 202 in Development, proxy likely couldn't forward downstream but accepted payload.",
        )
        print("hint", f"For deeper debugging, compare with direct Aspire OTLP: {aspire_otlp_url}")
        return 0
    except urllib.error.HTTPError as e:
        body = e.read()
        print("target", api_proxy_url)
        print("status", e.code)
        print("content-type", e.headers.get("content-type"))
        print("x-otlp-proxy-has-key", e.headers.get("x-otlp-proxy-has-key"))
        print("x-otlp-proxy-key-sha256", e.headers.get("x-otlp-proxy-key-sha256"))
        print("body_len", len(body))
        print(
            "note",
            "If status is 401/403, Aspire dashboard likely requires x-otlp-api-key and proxy didn't forward it.",
        )
        return 1
    except Exception as e:
        print("target", api_proxy_url)
        print("status", "error")
        print("exception", f"{type(e).__name__}: {e}")
        return 3


if __name__ == "__main__":
    raise SystemExit(main())
