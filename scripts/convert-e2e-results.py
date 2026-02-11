#!/usr/bin/env python3
"""
Convert js-framework-benchmark results to github-action-benchmark format.

The benchmark-action/github-action-benchmark action expects a specific JSON format
for the 'customSmallerIsBetter' tool. This script converts the js-framework-benchmark
result files into that format.

Usage:
    python scripts/convert-e2e-results.py --results-dir PATH --output PATH [--framework NAME]
"""

import argparse
import json
import sys
from pathlib import Path
from datetime import datetime


def find_result_files(results_dir: Path, framework: str) -> list[Path]:
    """Find all result JSON files for the specified framework."""
    pattern = f"{framework}*.json"
    files = list(results_dir.glob(pattern))
    return sorted(files)


def parse_result_file(file_path: Path) -> dict | None:
    """Parse a single js-framework-benchmark result file."""
    try:
        with open(file_path) as f:
            data = json.load(f)

        if "values" not in data:
            return None

        values = data["values"]
        if not values:
            return None

        median = sorted(values)[len(values) // 2]
        mean = sum(values) / len(values)

        # Extract benchmark name from filename
        # Format: framework_benchmarkname.json or framework-version_benchmarkname.json
        stem = file_path.stem
        parts = stem.split("_")
        if len(parts) >= 2:
            # Join all parts after the first underscore as benchmark name
            benchmark_name = "_".join(parts[1:])
        else:
            benchmark_name = stem

        return {
            "name": benchmark_name,
            "median": median,
            "mean": mean,
            "values": values,
        }
    except (json.JSONDecodeError, KeyError, IndexError) as e:
        print(f"Warning: Could not parse {file_path}: {e}", file=sys.stderr)
        return None


def convert_to_benchmark_format(results: list[dict]) -> list[dict]:
    """
    Convert results to github-action-benchmark customSmallerIsBetter format.

    Expected output format:
    [
        {
            "name": "01_run1k",
            "unit": "ms",
            "value": 92.5
        },
        ...
    ]
    """
    benchmark_results = []

    for result in results:
        benchmark_results.append({
            "name": result["name"],
            "unit": "ms",
            "value": result["median"],
            "extra": f"mean: {result['mean']:.1f}ms, samples: {len(result['values'])}"
        })

    return benchmark_results


def main():
    parser = argparse.ArgumentParser(
        description="Convert js-framework-benchmark results to github-action-benchmark format"
    )
    parser.add_argument(
        "--results-dir",
        type=Path,
        required=True,
        help="Directory containing js-framework-benchmark result JSON files"
    )
    parser.add_argument(
        "--output",
        type=Path,
        required=True,
        help="Output JSON file path"
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
    results = []
    for file_path in result_files:
        result = parse_result_file(file_path)
        if result:
            results.append(result)

    if not results:
        print("❌ No valid results parsed")
        sys.exit(1)

    print(f"Parsed {len(results)} benchmark results")

    # Convert to benchmark format
    benchmark_data = convert_to_benchmark_format(results)

    # Write output
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with open(args.output, "w") as f:
        json.dump(benchmark_data, f, indent=2)

    print(f"✅ Wrote {len(benchmark_data)} results to {args.output}")

    # Print summary
    print("\n## E2E Benchmark Results\n")
    print("| Benchmark | Median | Mean |")
    print("|-----------|--------|------|")
    for result in sorted(results, key=lambda r: r["name"]):
        print(f"| {result['name']} | {result['median']:.1f}ms | {result['mean']:.1f}ms |")


if __name__ == "__main__":
    main()
