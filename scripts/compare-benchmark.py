#!/usr/bin/env python3
"""
Benchmark Comparison Script for Abies Framework

Compares js-framework-benchmark results against baseline values.
Exits with non-zero code if regression is detected.

Usage:
    python scripts/compare-benchmark.py [--results-dir PATH] [--baseline PATH] [--threshold PERCENT]

Arguments:
    --results-dir   Directory containing benchmark result JSON files (default: webdriver-ts/results)
    --baseline      Path to baseline JSON file (default: benchmark-results/baseline.json)
    --threshold     Regression threshold percentage (default: 5.0)
    --update-baseline  Update baseline with current results instead of comparing
"""

import argparse
import json
import os
import statistics
import sys
from pathlib import Path
from dataclasses import dataclass
from typing import Optional


@dataclass
class BenchmarkResult:
    """Represents a single benchmark result."""
    name: str
    median: float
    mean: float
    std_dev: float
    values: list[float]


@dataclass
class Comparison:
    """Represents comparison between current and baseline."""
    name: str
    current: float
    baseline: float
    diff_percent: float
    is_regression: bool
    is_improvement: bool


def find_result_files(results_dir: Path, framework: str = "abies") -> list[Path]:
    """Find all result JSON files for the specified framework."""
    pattern = f"{framework}*.json"
    files = list(results_dir.glob(pattern))
    return sorted(files)


def parse_result_file(file_path: Path) -> Optional[BenchmarkResult]:
    """Parse a single benchmark result file."""
    try:
        with open(file_path) as f:
            data = json.load(f)

        # js-framework-benchmark format for CPU benchmarks:
        # {
        #   "framework": "abies-keyed",
        #   "benchmark": "01_run1k",
        #   "type": "cpu",
        #   "values": {
        #     "total": {"min": ..., "max": ..., "mean": ..., "median": ..., "values": [...]},
        #     "script": {...},
        #     "paint": {...}
        #   }
        # }
        if "values" in data:
            values_obj = data["values"]

            # Handle nested format (CPU benchmarks)
            if isinstance(values_obj, dict):
                # Use "total" timing as the primary metric
                if "total" in values_obj:
                    total_data = values_obj["total"]
                    # The stats object has pre-computed values
                    if isinstance(total_data, dict):
                        values = total_data.get("values", [])
                        median = total_data.get("median", statistics.median(values) if values else 0)
                        mean = total_data.get("mean", statistics.mean(values) if values else 0)
                        std_dev = total_data.get("stddev", statistics.stdev(values) if len(values) > 1 else 0)
                    else:
                        # Legacy format: values is directly an array
                        values = total_data if isinstance(total_data, list) else []
                        median = statistics.median(values) if values else 0
                        mean = statistics.mean(values) if values else 0
                        std_dev = statistics.stdev(values) if len(values) > 1 else 0
                elif "DEFAULT" in values_obj:
                    # Memory/startup benchmarks use DEFAULT key
                    default_data = values_obj["DEFAULT"]
                    if isinstance(default_data, dict):
                        values = default_data.get("values", [])
                        median = default_data.get("median", statistics.median(values) if values else 0)
                        mean = default_data.get("mean", statistics.mean(values) if values else 0)
                        std_dev = default_data.get("stddev", statistics.stdev(values) if len(values) > 1 else 0)
                    else:
                        values = default_data if isinstance(default_data, list) else []
                        median = statistics.median(values) if values else 0
                        mean = statistics.mean(values) if values else 0
                        std_dev = statistics.stdev(values) if len(values) > 1 else 0
                else:
                    print(f"Warning: Unknown values format in {file_path}", file=sys.stderr)
                    return None
            else:
                # Legacy format: values is directly an array
                values = values_obj
                median = statistics.median(values)
                mean = statistics.mean(values)
                std_dev = statistics.stdev(values) if len(values) > 1 else 0.0

            # Extract benchmark name from filename
            # Format: framework_benchmarkname.json
            name = file_path.stem.split("_", 1)[1] if "_" in file_path.stem else file_path.stem

            return BenchmarkResult(
                name=name,
                median=median,
                mean=mean,
                std_dev=std_dev,
                values=values if isinstance(values, list) else []
            )
    except (json.JSONDecodeError, KeyError, IndexError) as e:
        print(f"Warning: Could not parse {file_path}: {e}", file=sys.stderr)

    return None


def load_baseline(baseline_path: Path) -> dict[str, float]:
    """Load baseline values from JSON file."""
    if not baseline_path.exists():
        return {}

    try:
        with open(baseline_path) as f:
            content = f.read().strip()
            if not content:
                return {}
            data = json.loads(content)
        return data.get("benchmarks", {})
    except json.JSONDecodeError as e:
        print(f"Warning: Could not parse baseline file {baseline_path}: {e}", file=sys.stderr)
        return {}


def save_baseline(baseline_path: Path, results: dict[str, BenchmarkResult]) -> None:
    """Save current results as new baseline."""
    baseline_path.parent.mkdir(parents=True, exist_ok=True)

    data = {
        "version": "1.0",
        "framework": "abies",
        "benchmarks": {
            name: result.median
            for name, result in results.items()
        }
    }

    with open(baseline_path, "w") as f:
        json.dump(data, f, indent=2)

    print(f"✅ Baseline saved to {baseline_path}")


def compare_results(
    current: dict[str, BenchmarkResult],
    baseline: dict[str, float],
    threshold: float
) -> list[Comparison]:
    """Compare current results against baseline."""
    comparisons = []

    for name, result in current.items():
        if name not in baseline:
            continue

        baseline_value = baseline[name]
        current_value = result.median
        diff_percent = ((current_value - baseline_value) / baseline_value) * 100

        comparisons.append(Comparison(
            name=name,
            current=current_value,
            baseline=baseline_value,
            diff_percent=diff_percent,
            is_regression=diff_percent > threshold,
            is_improvement=diff_percent < -threshold
        ))

    return comparisons


def format_comparison_table(comparisons: list[Comparison]) -> str:
    """Format comparison results as a markdown table."""
    lines = [
        "| Benchmark | Baseline | Current | Diff | Status |",
        "|-----------|----------|---------|------|--------|",
    ]

    for comp in sorted(comparisons, key=lambda c: c.name):
        if comp.is_regression:
            status = "🔴 REGRESSION"
        elif comp.is_improvement:
            status = "🟢 Improved"
        else:
            status = "⚪ OK"

        diff_str = f"+{comp.diff_percent:.1f}%" if comp.diff_percent > 0 else f"{comp.diff_percent:.1f}%"

        lines.append(
            f"| {comp.name} | {comp.baseline:.1f}ms | {comp.current:.1f}ms | {diff_str} | {status} |"
        )

    return "\n".join(lines)


def main():
    parser = argparse.ArgumentParser(description="Compare js-framework-benchmark results")
    parser.add_argument(
        "--results-dir",
        type=Path,
        default=Path("webdriver-ts/results"),
        help="Directory containing benchmark result JSON files"
    )
    parser.add_argument(
        "--baseline",
        type=Path,
        default=Path("benchmark-results/baseline.json"),
        help="Path to baseline JSON file"
    )
    parser.add_argument(
        "--threshold",
        type=float,
        default=5.0,
        help="Regression threshold percentage (default: 5.0)"
    )
    parser.add_argument(
        "--update-baseline",
        action="store_true",
        help="Update baseline with current results instead of comparing"
    )
    parser.add_argument(
        "--framework",
        type=str,
        default="abies",
        help="Framework name to look for in results (default: abies)"
    )

    args = parser.parse_args()

    # Find and parse result files
    result_files = find_result_files(args.results_dir, args.framework)

    if not result_files:
        print(f"❌ No result files found in {args.results_dir} for framework '{args.framework}'")
        sys.exit(1)

    print(f"Found {len(result_files)} result files")

    # Parse results
    results: dict[str, BenchmarkResult] = {}
    for file_path in result_files:
        result = parse_result_file(file_path)
        if result:
            results[result.name] = result

    if not results:
        print("❌ No valid results parsed")
        sys.exit(1)

    print(f"Parsed {len(results)} benchmark results")

    # Update baseline mode
    if args.update_baseline:
        save_baseline(args.baseline, results)
        return

    # Compare mode
    baseline = load_baseline(args.baseline)

    if not baseline:
        print(f"⚠️  No baseline found at {args.baseline}")
        print("This is expected for the first run - baseline will be created after merge to main")
        print("\nCurrent results (no comparison available):")
        for name, result in sorted(results.items()):
            print(f"  {name}: {result.median:.1f}ms (±{result.std_dev:.1f}ms)")
        # First run without baseline is OK - just report results
        # Baseline will be created when merged to main and stored in gh-pages
        print("\n✅ First run completed - results will be used as baseline after merge")
        sys.exit(0)

    comparisons = compare_results(results, baseline, args.threshold)

    if not comparisons:
        print("⚠️  No matching benchmarks to compare")
        sys.exit(0)

    # Output results
    print("\n## Benchmark Comparison Results\n")
    print(format_comparison_table(comparisons))
    print()

    # Check for regressions
    regressions = [c for c in comparisons if c.is_regression]
    improvements = [c for c in comparisons if c.is_improvement]

    if improvements:
        print(f"🎉 {len(improvements)} benchmark(s) improved!")

    if regressions:
        print(f"\n❌ {len(regressions)} regression(s) detected (>{args.threshold}% slower):")
        for reg in regressions:
            print(f"   - {reg.name}: {reg.baseline:.1f}ms → {reg.current:.1f}ms ({reg.diff_percent:+.1f}%)")
        sys.exit(1)

    print("\n✅ No regressions detected")


if __name__ == "__main__":
    main()
