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


# Benchmarks we care about (js-framework-benchmark naming)
TRACKED_BENCHMARKS = [
    "01_run1k",
    "02_replace1k",
    "03_update10th1k",
    "04_select1k",
    "05_swap1k",
    "06_remove-one-1k",
    "07_create10k",
    "08_create1k-after1k_x2",
    "09_clear1k",
]

# Target ratios vs Blazor (from benchmarking-strategy.md)
PERFORMANCE_TARGETS = {
    "01_run1k": 1.05,      # ≤1.05x Blazor
    "05_swap1k": 1.5,      # ≤1.5x Blazor
    "09_clear1k": 2.0,     # ≤2x Blazor
}


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

        # js-framework-benchmark format
        if "values" in data:
            values = data["values"]
            median = sorted(values)[len(values) // 2]
            mean = sum(values) / len(values)
            variance = sum((x - mean) ** 2 for x in values) / len(values)
            std_dev = variance ** 0.5

            # Extract benchmark name from filename
            # Format: framework_benchmarkname.json
            name = file_path.stem.split("_", 1)[1] if "_" in file_path.stem else file_path.stem

            return BenchmarkResult(
                name=name,
                median=median,
                mean=mean,
                std_dev=std_dev,
                values=values
            )
    except (json.JSONDecodeError, KeyError, IndexError) as e:
        print(f"Warning: Could not parse {file_path}: {e}", file=sys.stderr)

    return None


def load_baseline(baseline_path: Path) -> dict[str, float]:
    """Load baseline values from JSON file."""
    if not baseline_path.exists():
        return {}

    with open(baseline_path) as f:
        data = json.load(f)

    return data.get("benchmarks", {})


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
        print("Run with --update-baseline to create initial baseline")
        print("\nCurrent results:")
        for name, result in sorted(results.items()):
            print(f"  {name}: {result.median:.1f}ms (±{result.std_dev:.1f}ms)")
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
