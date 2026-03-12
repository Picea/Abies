#!/usr/bin/env python3
"""
Update README.md benchmark tables with latest CI results.

Reads js-framework-benchmark result files and a static Blazor baseline,
then regenerates the benchmark comparison tables in README.md between
HTML comment markers.

Only updates Duration (01-09) and Memory (21, 22, 25) tables.
Startup/Size is not measured in CI and stays static.

Usage:
    python scripts/update-readme-benchmarks.py \\
        --results-dir js-framework-benchmark/webdriver-ts/results \\
        --blazor-baseline benchmark-results/blazor-baseline.json \\
        --readme README.md \\
        --framework abies
"""

import argparse
import json
import math
import re
import sys
from pathlib import Path


# ============================================================================
# Benchmark definitions — map result file names to README labels
# ============================================================================

DURATION_BENCHMARKS = [
    ("01_run1k", "Create 1,000 rows"),
    ("02_replace1k", "Replace 1,000 rows"),
    ("03_update10th1k", "Update every 10th row ×16"),
    ("04_select1k", "Select row"),
    ("05_swap1k", "Swap rows"),
    ("06_remove-one-1k", "Remove row"),
    ("07_create10k", "Create 10,000 rows"),
    ("08_create1k-after1k_x2", "Append 1,000 rows"),
    ("09_clear1k", "Clear 1,000 rows ×8"),
]

MEMORY_BENCHMARKS = [
    ("21_ready-memory", "Ready memory"),
    ("22_run-memory", "Run memory"),
    ("25_clear-memory", "Run-clear memory"),
]


# ============================================================================
# Result file parsing (same logic as convert-e2e-results.py)
# ============================================================================

def find_result_files(results_dir: Path, framework: str) -> list[Path]:
    """Find all result JSON files for the specified framework."""
    pattern = f"{framework}*.json"
    return sorted(results_dir.glob(pattern))


def parse_result_file(file_path: Path) -> dict | None:
    """Parse a single js-framework-benchmark result file.

    Returns dict with 'name' and 'median' keys, or None on failure.
    """
    try:
        with open(file_path) as f:
            data = json.load(f)

        if "values" not in data:
            return None

        values_obj = data["values"]
        if not values_obj:
            return None

        # Handle nested format (CPU benchmarks use "total", memory uses "DEFAULT")
        if isinstance(values_obj, dict):
            for key in ("total", "DEFAULT"):
                if key in values_obj:
                    inner = values_obj[key]
                    if isinstance(inner, dict):
                        values = inner.get("values", [])
                        median = inner.get(
                            "median",
                            sorted(values)[len(values) // 2] if values else 0,
                        )
                    else:
                        values = inner if isinstance(inner, list) else []
                        median = sorted(values)[len(values) // 2] if values else 0
                    break
            else:
                print(f"Warning: Unknown values format in {file_path}", file=sys.stderr)
                return None
        else:
            values = values_obj
            median = sorted(values)[len(values) // 2] if values else 0

        # Extract benchmark name from filename
        # e.g. abies-keyed_01_run1k.json -> 01_run1k
        stem = file_path.stem
        parts = stem.split("_", 1)
        benchmark_name = parts[1] if len(parts) >= 2 else stem

        return {"name": benchmark_name, "median": median}

    except (json.JSONDecodeError, KeyError, IndexError) as e:
        print(f"Warning: Could not parse {file_path}: {e}", file=sys.stderr)
        return None


# ============================================================================
# Table generation
# ============================================================================

def compute_delta(abies: float, blazor: float) -> str:
    """Compute percentage delta string for the table."""
    if blazor == 0:
        return "—"
    pct = (abies - blazor) / blazor * 100
    if pct < -0.5:
        return f"**\u2212{abs(pct):.0f}%**"
    if pct > 0.5:
        return f"+{pct:.0f}%"
    return "—"


def format_value(value: float, unit: str, bold: bool = False) -> str:
    """Format a benchmark value with unit and optional bold."""
    if unit == "ms":
        text = f"{value:,.1f} ms"
    elif unit == "MB":
        text = f"{value:.1f} MB"
    else:
        text = f"{value}"
    return f"**{text}**" if bold else text


def generate_duration_table(
    results: dict[str, dict], blazor: dict[str, float]
) -> str:
    """Generate the Duration Benchmarks markdown table."""
    lines = [
        "| Benchmark | Abies 2.0 | Blazor 10.0 | Delta |",
        "| --- | --- | --- | --- |",
    ]

    abies_medians: list[float] = []
    blazor_medians: list[float] = []

    for bench_id, label in DURATION_BENCHMARKS:
        abies_val = results.get(bench_id, {}).get("median")
        blazor_val = blazor.get(bench_id)

        if abies_val is None or blazor_val is None:
            continue

        abies_medians.append(abies_val)
        blazor_medians.append(blazor_val)

        abies_wins = abies_val <= blazor_val
        abies_str = format_value(abies_val, "ms", bold=abies_wins)
        blazor_str = format_value(blazor_val, "ms", bold=not abies_wins)
        delta = compute_delta(abies_val, blazor_val)

        lines.append(f"| {label} | {abies_str} | {blazor_str} | {delta} |")

    # Geometric mean row
    if abies_medians and blazor_medians:
        abies_geo = math.exp(
            sum(math.log(v) for v in abies_medians) / len(abies_medians)
        )
        blazor_geo = math.exp(
            sum(math.log(v) for v in blazor_medians) / len(blazor_medians)
        )
        ratio = blazor_geo / abies_geo
        lines.append(f"| **Geometric mean** | **1.00\u00d7** | **{ratio:.2f}\u00d7** | |")

    return "\n".join(lines)


def generate_memory_table(
    results: dict[str, dict], blazor: dict[str, float]
) -> str:
    """Generate the Memory markdown table."""
    lines = [
        "| Metric | Abies 2.0 | Blazor 10.0 | Delta |",
        "| --- | --- | --- | --- |",
    ]

    for bench_id, label in MEMORY_BENCHMARKS:
        abies_val = results.get(bench_id, {}).get("median")
        blazor_val = blazor.get(bench_id)

        if abies_val is None or blazor_val is None:
            continue

        abies_wins = abies_val <= blazor_val
        abies_str = format_value(abies_val, "MB", bold=abies_wins)
        blazor_str = format_value(blazor_val, "MB", bold=not abies_wins)
        delta = compute_delta(abies_val, blazor_val)

        lines.append(f"| {label} | {abies_str} | {blazor_str} | {delta} |")

    return "\n".join(lines)


# ============================================================================
# README marker replacement
# ============================================================================

def replace_between_markers(
    content: str, start_marker: str, end_marker: str, new_content: str
) -> str:
    """Replace content between HTML comment markers in a string."""
    pattern = re.compile(
        f"({re.escape(start_marker)}\n)(.*?)(\n{re.escape(end_marker)})",
        re.DOTALL,
    )
    replacement = f"\\1{new_content}\\3"
    result, count = pattern.subn(replacement, content)
    if count == 0:
        print(
            f"Warning: Markers not found: {start_marker} / {end_marker}",
            file=sys.stderr,
        )
    return result


# ============================================================================
# Main
# ============================================================================

def main():
    parser = argparse.ArgumentParser(
        description="Update README.md benchmark tables with latest CI results"
    )
    parser.add_argument(
        "--results-dir",
        type=Path,
        required=True,
        help="Directory containing js-framework-benchmark result JSON files",
    )
    parser.add_argument(
        "--blazor-baseline",
        type=Path,
        required=True,
        help="Path to blazor-baseline.json with static Blazor comparison numbers",
    )
    parser.add_argument(
        "--readme",
        type=Path,
        required=True,
        help="Path to README.md to update in-place",
    )
    parser.add_argument(
        "--framework",
        type=str,
        default="abies",
        help="Framework name to look for in results (default: abies)",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Print updated tables without modifying README.md",
    )
    args = parser.parse_args()

    # ------------------------------------------------------------------
    # 1. Load Blazor baseline
    # ------------------------------------------------------------------
    if not args.blazor_baseline.exists():
        print(f"\u274c Blazor baseline not found: {args.blazor_baseline}", file=sys.stderr)
        sys.exit(1)

    with open(args.blazor_baseline) as f:
        blazor_baseline = json.load(f)

    blazor_duration = blazor_baseline.get("duration", {})
    blazor_memory = blazor_baseline.get("memory", {})

    # ------------------------------------------------------------------
    # 2. Find and parse result files
    # ------------------------------------------------------------------
    result_files = find_result_files(args.results_dir, args.framework)
    if not result_files:
        print(
            f"\u274c No result files found in {args.results_dir} for '{args.framework}'",
            file=sys.stderr,
        )
        sys.exit(1)

    results: dict[str, dict] = {}
    for file_path in result_files:
        parsed = parse_result_file(file_path)
        if parsed:
            results[parsed["name"]] = parsed

    print(f"Parsed {len(results)} benchmark results")

    # ------------------------------------------------------------------
    # 3. Generate tables
    # ------------------------------------------------------------------
    duration_table = generate_duration_table(results, blazor_duration)
    memory_table = generate_memory_table(results, blazor_memory)

    if args.dry_run:
        print("\n### Duration Benchmarks\n")
        print(duration_table)
        print("\n### Memory\n")
        print(memory_table)
        return

    # ------------------------------------------------------------------
    # 4. Update README.md
    # ------------------------------------------------------------------
    if not args.readme.exists():
        print(f"\u274c README not found: {args.readme}", file=sys.stderr)
        sys.exit(1)

    content = args.readme.read_text()
    original = content

    content = replace_between_markers(
        content,
        "<!-- BENCHMARK:DURATION:START -->",
        "<!-- BENCHMARK:DURATION:END -->",
        duration_table,
    )

    content = replace_between_markers(
        content,
        "<!-- BENCHMARK:MEMORY:START -->",
        "<!-- BENCHMARK:MEMORY:END -->",
        memory_table,
    )

    if content == original:
        print("\u2139\ufe0f  README.md unchanged")
    else:
        args.readme.write_text(content)
        print("\u2705 README.md updated with latest benchmark results")


if __name__ == "__main__":
    main()
