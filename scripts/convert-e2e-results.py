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

        values_obj = data["values"]
        if not values_obj:
            return None

        # Handle nested format (CPU benchmarks)
        # Format: {"total": {"min": ..., "values": [...]}, "script": {...}, "paint": {...}}
        if isinstance(values_obj, dict):
            # Use "total" timing as the primary metric
            if "total" in values_obj:
                total_data = values_obj["total"]
                if isinstance(total_data, dict):
                    values = total_data.get("values", [])
                    median = total_data.get("median", sorted(values)[len(values) // 2] if values else 0)
                    mean = total_data.get("mean", sum(values) / len(values) if values else 0)
                else:
                    values = total_data if isinstance(total_data, list) else []
                    median = sorted(values)[len(values) // 2] if values else 0
                    mean = sum(values) / len(values) if values else 0
            elif "DEFAULT" in values_obj:
                # Memory/startup benchmarks use DEFAULT key
                default_data = values_obj["DEFAULT"]
                if isinstance(default_data, dict):
                    values = default_data.get("values", [])
                    median = default_data.get("median", sorted(values)[len(values) // 2] if values else 0)
                    mean = default_data.get("mean", sum(values) / len(values) if values else 0)
                else:
                    values = default_data if isinstance(default_data, list) else []
                    median = sorted(values)[len(values) // 2] if values else 0
                    mean = sum(values) / len(values) if values else 0
            else:
                print(f"Warning: Unknown values format in {file_path}", file=sys.stderr)
                return None
        else:
            # Legacy format: values is directly an array
            values = values_obj
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
            "values": values if isinstance(values, list) else [],
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
            "name": "01_run1k (create 1000 rows)",
            "unit": "ms",
            "value": 92.5
        },
        ...
    ]

    Each benchmark gets its own trend line in gh-pages.
    """
    # Human-readable descriptions for key benchmarks
    benchmark_descriptions = {
        "01_run1k": "create 1000 rows",
        "02_replace1k": "replace all 1000 rows",
        "03_update10th1k": "update every 10th row",
        "04_select1k": "select row",
        "05_swap1k": "swap two rows",
        "06_remove-one-1k": "remove one row",
        "07_create10k": "create 10,000 rows",
        "08_create1k-after1k_x2": "create 1k after 1k",
        "09_clear1k": "clear all rows",
    }

    benchmark_results = []

    for result in results:
        name = result["name"]
        description = benchmark_descriptions.get(name, "")
        display_name = f"{name} ({description})" if description else name

        benchmark_results.append({
            "name": display_name,
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
