#!/usr/bin/env python3
"""
Compare benchmark results between baseline (main) and PR branches.

This script performs same-job comparison to avoid CI runner variance between runs.
Both baseline and PR benchmarks are run in the same job, then compared here.

Usage:
    python compare-benchmarks.py <baseline-dir> <pr-dir> [--throughput-threshold 110] [--allocation-threshold 120]

Arguments:
    baseline-dir         Directory containing baseline (main) merged benchmark results
    pr-dir               Directory containing PR merged benchmark results
    --throughput-threshold  Fail if throughput regresses by more than this % (default: 110 = 10% slower)
    --allocation-threshold  Fail if allocations increase by more than this % (default: 120 = 20% more)

Exit codes:
    0 - All benchmarks pass thresholds
    1 - One or more benchmarks exceed thresholds (regression detected)
    2 - Error (missing files, invalid data, etc.)
"""

import argparse
import json
import sys
from pathlib import Path
from dataclasses import dataclass
from typing import Optional


@dataclass
class BenchmarkComparison:
    """Result of comparing a single benchmark between baseline and PR."""
    name: str
    baseline_value: float
    pr_value: float
    unit: str
    ratio: float  # PR / Baseline (>1 means regression for time, <1 for throughput)
    threshold: float
    passed: bool
    is_throughput: bool  # True = lower is better (time), False = lower is better (allocations)


def load_throughput_metrics(merged_dir: Path) -> dict[str, float]:
    """Load throughput metrics from merged BenchmarkDotNet format."""
    file_path = merged_dir / 'throughput.json'
    if not file_path.exists():
        return {}

    with open(file_path, 'r') as f:
        data = json.load(f)

    metrics = {}
    for benchmark in data.get('Benchmarks', []):
        name = benchmark.get('Method', 'Unknown')
        stats = benchmark.get('Statistics', {})
        mean = stats.get('Mean', 0)  # nanoseconds
        if mean > 0:
            metrics[name] = mean

    return metrics


def load_allocation_metrics(merged_dir: Path) -> dict[str, float]:
    """Load allocation metrics from customSmallerIsBetter format."""
    file_path = merged_dir / 'allocations.json'
    if not file_path.exists():
        return {}

    with open(file_path, 'r') as f:
        data = json.load(f)

    metrics = {}
    for item in data:
        name = item.get('name', 'Unknown')
        value = item.get('value', 0)
        metrics[name] = value

    return metrics


def compare_metrics(
    baseline: dict[str, float],
    pr: dict[str, float],
    threshold_percent: float,
    unit: str,
    is_throughput: bool
) -> list[BenchmarkComparison]:
    """Compare metrics between baseline and PR.

    For throughput (time-based): PR > Baseline means regression
    For allocations (memory): PR > Baseline means regression

    Both cases use ratio = PR / Baseline, and ratio > threshold means failure.
    """
    results = []

    # Find common benchmarks
    common_names = set(baseline.keys()) & set(pr.keys())

    for name in sorted(common_names):
        baseline_val = baseline[name]
        pr_val = pr[name]

        if baseline_val <= 0:
            continue  # Skip invalid baselines

        ratio = pr_val / baseline_val
        ratio_percent = ratio * 100

        # Regression if ratio exceeds threshold (e.g., 110% = 10% slower/more)
        passed = ratio_percent <= threshold_percent

        results.append(BenchmarkComparison(
            name=name,
            baseline_value=baseline_val,
            pr_value=pr_val,
            unit=unit,
            ratio=ratio,
            threshold=threshold_percent,
            passed=passed,
            is_throughput=is_throughput
        ))

    return results


def format_value(value: float, unit: str) -> str:
    """Format a benchmark value with appropriate units."""
    if unit == 'ns':
        if value >= 1_000_000_000:
            return f"{value / 1_000_000_000:.2f} s"
        elif value >= 1_000_000:
            return f"{value / 1_000_000:.2f} ms"
        elif value >= 1_000:
            return f"{value / 1_000:.2f} μs"
        else:
            return f"{value:.2f} ns"
    elif unit == 'bytes':
        if value >= 1_073_741_824:  # 1 GiB
            return f"{value / 1_073_741_824:.2f} GiB"
        elif value >= 1_048_576:  # 1 MiB
            return f"{value / 1_048_576:.2f} MiB"
        elif value >= 1024:  # 1 KiB
            return f"{value / 1024:.2f} KiB"
        else:
            return f"{value:.0f} B"
    else:
        return f"{value:.2f} {unit}"


def print_comparison_table(comparisons: list[BenchmarkComparison], title: str) -> tuple[int, int]:
    """Print a comparison table and return (passed_count, failed_count)."""
    if not comparisons:
        print(f"\n{title}: No benchmarks to compare")
        return 0, 0

    print(f"\n{'='*80}")
    print(f"{title}")
    print(f"{'='*80}")

    # Header
    print(f"{'Benchmark':<40} {'Baseline':>12} {'PR':>12} {'Change':>10} {'Status':>8}")
    print(f"{'-'*40} {'-'*12} {'-'*12} {'-'*10} {'-'*8}")

    passed = 0
    failed = 0

    for comp in comparisons:
        baseline_str = format_value(comp.baseline_value, comp.unit)
        pr_str = format_value(comp.pr_value, comp.unit)

        # Calculate percentage change
        change_percent = (comp.ratio - 1) * 100
        if change_percent >= 0:
            change_str = f"+{change_percent:.1f}%"
        else:
            change_str = f"{change_percent:.1f}%"

        status = "✓ PASS" if comp.passed else "✗ FAIL"

        # Truncate long names
        name = comp.name[:38] + '..' if len(comp.name) > 40 else comp.name

        print(f"{name:<40} {baseline_str:>12} {pr_str:>12} {change_str:>10} {status:>8}")

        if comp.passed:
            passed += 1
        else:
            failed += 1

    return passed, failed


def main():
    parser = argparse.ArgumentParser(
        description='Compare benchmark results between baseline and PR'
    )
    parser.add_argument('baseline_dir', type=Path, help='Baseline (main) merged results directory')
    parser.add_argument('pr_dir', type=Path, help='PR merged results directory')
    parser.add_argument(
        '--throughput-threshold',
        type=float,
        default=110.0,
        help='Fail if throughput regresses by more than this %% (default: 110 = 10%% slower)'
    )
    parser.add_argument(
        '--allocation-threshold',
        type=float,
        default=120.0,
        help='Fail if allocations increase by more than this %% (default: 120 = 20%% more)'
    )

    args = parser.parse_args()

    # Validate directories
    if not args.baseline_dir.exists():
        print(f"Error: Baseline directory not found: {args.baseline_dir}")
        sys.exit(2)

    if not args.pr_dir.exists():
        print(f"Error: PR directory not found: {args.pr_dir}")
        sys.exit(2)

    print("="*80)
    print("SAME-JOB BENCHMARK COMPARISON")
    print("="*80)
    print(f"Baseline directory: {args.baseline_dir}")
    print(f"PR directory:       {args.pr_dir}")
    print(f"Throughput threshold: {args.throughput_threshold}% (fail if slower)")
    print(f"Allocation threshold: {args.allocation_threshold}% (fail if more)")

    # Load metrics
    baseline_throughput = load_throughput_metrics(args.baseline_dir)
    pr_throughput = load_throughput_metrics(args.pr_dir)

    baseline_allocations = load_allocation_metrics(args.baseline_dir)
    pr_allocations = load_allocation_metrics(args.pr_dir)

    if not baseline_throughput and not baseline_allocations:
        print(f"\nError: No baseline metrics found in {args.baseline_dir}")
        sys.exit(2)

    if not pr_throughput and not pr_allocations:
        print(f"\nError: No PR metrics found in {args.pr_dir}")
        sys.exit(2)

    # Compare throughput (lower is better - time in nanoseconds)
    throughput_comparisons = compare_metrics(
        baseline_throughput,
        pr_throughput,
        args.throughput_threshold,
        'ns',
        is_throughput=True
    )

    # Compare allocations (lower is better)
    allocation_comparisons = compare_metrics(
        baseline_allocations,
        pr_allocations,
        args.allocation_threshold,
        'bytes',
        is_throughput=False
    )

    # Print results
    tp_passed, tp_failed = print_comparison_table(
        throughput_comparisons,
        f"THROUGHPUT COMPARISON (threshold: {args.throughput_threshold}%)"
    )

    alloc_passed, alloc_failed = print_comparison_table(
        allocation_comparisons,
        f"ALLOCATION COMPARISON (threshold: {args.allocation_threshold}%)"
    )

    # Summary
    total_passed = tp_passed + alloc_passed
    total_failed = tp_failed + alloc_failed

    print(f"\n{'='*80}")
    print("SUMMARY")
    print(f"{'='*80}")
    print(f"Throughput:  {tp_passed} passed, {tp_failed} failed")
    print(f"Allocations: {alloc_passed} passed, {alloc_failed} failed")
    print(f"Total:       {total_passed} passed, {total_failed} failed")

    if total_failed > 0:
        print(f"\n❌ BENCHMARK CHECK FAILED - {total_failed} regression(s) detected")
        sys.exit(1)
    else:
        print(f"\n✅ BENCHMARK CHECK PASSED - No regressions detected")
        sys.exit(0)


if __name__ == '__main__':
    main()
