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


def is_memory_benchmark(name: str) -> bool:
    """Check if a benchmark name corresponds to a memory benchmark."""
    return name.startswith(("21_", "22_", "23_", "24_", "25_", "26_"))


def convert_to_benchmark_format(results: list[dict], memory_only: bool = False) -> list[dict]:
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

    Args:
        results: Parsed benchmark results.
        memory_only: If True, only include memory benchmarks (21-26) with MB unit.
                     If False, only include CPU benchmarks (01-09) with ms unit.
    """
    # Human-readable descriptions for all benchmarks
    benchmark_descriptions = {
        # CPU benchmarks (01-09)
        "01_run1k": "create 1000 rows",
        "02_replace1k": "replace all 1000 rows",
        "03_update10th1k": "update every 10th row",
        "04_select1k": "select row",
        "05_swap1k": "swap two rows",
        "06_remove-one-1k": "remove one row",
        "07_create10k": "create 10,000 rows",
        "08_create1k-after1k_x2": "append 1000 rows",
        "09_clear1k": "clear all rows",
        # Memory benchmarks (21-26)
        "21_ready-memory": "ready memory",
        "22_run-memory": "run memory",
        "23_update5-memory": "update5 memory",
        "24_replace5-memory": "replace5 memory",
        "25_clear-memory": "clear memory",
        "26_run-clear-memory": "run-clear memory",
        # Startup benchmarks (31-34)
        "31_startup-ci": "startup time",
        "32_startup-bt": "script bootup time",
        "33_startup-mainthreadcost": "main thread work cost",
        "34_startup-totalbytes": "total byte weight",
    }

    unit = "MB" if memory_only else "ms"
    benchmark_results = []

    for result in results:
        name = result["name"]
        is_mem = is_memory_benchmark(name)

        # Filter: only include memory benchmarks if memory_only, else only CPU
        if memory_only and not is_mem:
            continue
        if not memory_only and is_mem:
            continue

        description = benchmark_descriptions.get(name, "")
        display_name = f"{name} ({description})" if description else name

        benchmark_results.append({
            "name": display_name,
            "unit": unit,
            "value": result["median"],
            "extra": f"mean: {result['mean']:.1f}{unit}, samples: {len(result['values'])}"
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
        help="Output JSON file path for CPU benchmarks (ms)"
    )
    parser.add_argument(
        "--output-memory",
        type=Path,
        default=None,
        help="Output JSON file path for memory benchmarks (MB). If omitted, memory benchmarks are skipped."
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

    # Convert CPU benchmarks (01-09) to benchmark format
    cpu_data = convert_to_benchmark_format(results, memory_only=False)

    # Write CPU output
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with open(args.output, "w") as f:
        json.dump(cpu_data, f, indent=2)

    print(f"✅ Wrote {len(cpu_data)} CPU results to {args.output}")

    # Write memory output if requested
    if args.output_memory:
        memory_data = convert_to_benchmark_format(results, memory_only=True)
        if memory_data:
            args.output_memory.parent.mkdir(parents=True, exist_ok=True)
            with open(args.output_memory, "w") as f:
                json.dump(memory_data, f, indent=2)
            print(f"✅ Wrote {len(memory_data)} memory results to {args.output_memory}")
        else:
            print("⚠️  No memory benchmarks found in results")

    # Print summary
    print("\n## E2E Benchmark Results\n")
    print("| Benchmark | Median | Mean | Unit |")
    print("|-----------|--------|------|------|")
    for result in sorted(results, key=lambda r: r["name"]):
        unit = "MB" if is_memory_benchmark(result["name"]) else "ms"
        print(f"| {result['name']} | {result['median']:.1f}{unit} | {result['mean']:.1f}{unit} | {unit} |")


if __name__ == "__main__":
    main()
