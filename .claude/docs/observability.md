# Observability

Claude Code emits OpenTelemetry traces, metrics, and logs natively. This document records the squad's recommended configuration, what to watch on dashboards, and where the data goes.

The goal is not "audit Claude" — it's "treat the agent as part of the production system." When a CI pipeline review suddenly takes 4× longer than usual, or the reviewer subagent's verdicts skew one way for a week, the telemetry is the only way to spot it without trawling logs.

## What's emitted

Three signal types via standard OTLP:

**Traces.** Spans for every interaction. Useful spans:

- `claude_code.interaction` — root span per user turn.
- `claude_code.llm_request` — child span per model call. Carries token counts and model id.
- `claude_code.tool` — child span per tool invocation. The most useful sub-spans:
  - `claude_code.tool.execution` — actual tool runtime.
  - `claude_code.tool.blocked_on_user` — time spent waiting for a permission prompt.
- `claude_code.hook` — span per hook execution. Requires `CLAUDE_CODE_ENHANCED_TELEMETRY_BETA=1`.

Subagent boundaries are visible as `claude_code.tool` spans where `tool_name=Task`. Use that to slice the squad's work by subagent.

**Metrics.** Counters and histograms covering sessions, lines added/removed, PRs, commits, total cost, total tokens, edit accept/reject ratio, and active time.

**Logs.** Structured logs for hooks, errors, and lifecycle events. Default is metadata-only — *prompt and tool-input content is not exported by default*. Don't change that unless the OTLP collector is private.

## Recommended configuration

In `.claude/settings.json` `env` block:

```json
{
  "env": {
    "CLAUDE_CODE_ENABLE_TELEMETRY": "1",
    "CLAUDE_CODE_ENHANCED_TELEMETRY_BETA": "1",
    "OTEL_METRICS_EXPORTER": "otlp",
    "OTEL_LOGS_EXPORTER": "otlp",
    "OTEL_TRACES_EXPORTER": "otlp",
    "OTEL_EXPORTER_OTLP_PROTOCOL": "grpc",
    "OTEL_EXPORTER_OTLP_ENDPOINT": "http://localhost:4317",
    "OTEL_METRIC_EXPORT_INTERVAL": "10000",
    "OTEL_RESOURCE_ATTRIBUTES": "service.name=claude-squad,deployment.environment=local"
  }
}
```

The endpoint should match the Aspire OTLP collector you already run. The squad's app and the squad's agent then live in the same observability plane — slice by `service.name` to keep dashboards clean.

This template's own CI workflows (`hook-tests.yml`, `template-bootstrap.yml`) don't invoke Claude Code, so there's no headless session to instrument in this repo today. If you add a CI workflow that runs a headless Claude Code session (an automated review gate, for example), override the resource attributes for that run:

```yaml
env:
  OTEL_RESOURCE_ATTRIBUTES: "service.name=claude-squad,deployment.environment=ci"
```

so headless CI runs are separable from interactive sessions on the dashboard.

## Privacy defaults

These remain **off** by default and you should leave them off unless the collector is private:

- `OTEL_LOG_USER_PROMPTS=1` — would export the user's prompt text. Prompts contain code, decisions, sometimes credentials — not for shared collectors.
- Tool input/output payload export — not enabled by default; same reason.

If you need full payload export (e.g., for replay debugging in a dev cluster), do it via a per-developer override file, not `.claude/settings.json` checked into git.

## Cardinality

Some metric attributes can balloon dashboard cardinality. Keep these disabled unless you have a specific dashboard that needs them:

- `OTEL_METRICS_INCLUDE_SESSION_ID=true` — adds session_id as a metric attribute. Useful for per-session debugging, expensive at scale.

Resource attributes (`service.name`, `deployment.environment`, `service.instance.id`) are fine.

## What to watch

Build dashboards for these — they're the leading indicators of squad-level problems:

1. **Reviewer verdict-time histogram.** `claude_code.tool` spans where `tool_name=Task` and the input contains `reviewer`. P50, P95, P99. Rising P95 → reviewer is bogging down on something specific (test failures? merge conflicts? a recurring blocker pattern?).

2. **Hook failure rate.** `claude_code.hook` spans where status is non-OK. Spike → a hook is failing silently (the squad's hooks all `exit 0` on internal errors; OTEL is the only way to see them).

3. **Subagent dispatch ratio.** Counter of `Task` tool calls grouped by target subagent. If one subagent suddenly stops being invoked (or another suddenly dominates), that's a signal that the orchestrator's routing has shifted — possibly drift, possibly a deliberate pattern shift, but worth knowing.

4. **Quarantine-drop rate.** Custom metric — emit one counter increment per quarantined drop in `scribe-decision-merger.sh`. Currently inferable from log scraping; emit explicitly if you want a clean dashboard. Spike → a subagent is producing malformed front-matter, which means it's reading an outdated charter or the schema doc has drifted.

5. **Token cost per session by user prompt category.** Use the skill-router's hint as a tag (you'd have to forward it through telemetry, which is more work — Tier 2).

6. **`claude_code.tool.blocked_on_user` total.** Time spent waiting for permission prompts. If this is big, sandboxing or pre-approved tool lists are worth investigating.

## SigNoz / Honeycomb / Datadog / Logfire

The squad does not prescribe a vendor. The OTLP endpoint is generic; SigNoz is a popular open-source choice for self-hosting, ColeMurray/claude-code-otel and TechNickAI/claude_telemetry are reference dashboards if you'd rather not roll your own.

## What this is NOT

Not a substitute for the squad's own state files. The session-logger hook still writes daily prose summaries; the scribe-decision-merger still produces durable decisions; the orchestration log still records Lead-level handoffs. OTEL gives you metrics and traces; the squad's `.squad/` directory gives you operational narrative. Both are needed.
