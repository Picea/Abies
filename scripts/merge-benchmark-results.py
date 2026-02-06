#!/usr/bin/env python3
"""
Merge benchmark results from multiple suites into unified files.

This script:
1. Reads BenchmarkDotNet JSON output from each benchmark suite
2. Extracts throughput and memory allocation metrics
3. Produces merged JSON files compatible with github-action-benchmark

Usage:
    python merge-benchmark-results.py <benchmark-results-dir>

Expected directory structure:
    benchmark-results/
        diffing/results/Abies.Benchmarks.DomDiffingBenchmarks-report-full-compressed.json
        rendering/results/Abies.Benchmarks.RenderingBenchmarks-report-full-compressed.json
        handlers/results/Abies.Benchmarks.EventHandlerBenchmarks-report-full-compressed.json

Output:
    benchmark-results/merged/
        throughput.json   - BenchmarkDotNet format for throughput metrics
        allocations.json  - customSmallerIsBetter format for memory allocations
"""

import json
import os
import sys
from pathlib import Path
from datetime import datetime


def find_benchmark_files(base_dir: Path) -> dict[str, Path]:
    """Find all benchmark JSON files in the results directory."""
    files = {}
    for suite in ['diffing', 'rendering', 'handlers']:
        suite_dir = base_dir / suite / 'results'
        if suite_dir.exists():
            for f in suite_dir.glob('*-report-full-compressed.json'):
                files[suite] = f
                break
    return files


def load_benchmark_data(file_path: Path) -> dict:
    """Load and parse BenchmarkDotNet JSON file."""
    with open(file_path, 'r') as f:
        return json.load(f)


def extract_throughput_metrics(data: dict, suite_prefix: str) -> list[dict]:
    """Extract throughput metrics in BenchmarkDotNet format."""
    benchmarks = []
    for benchmark in data.get('Benchmarks', []):
        # Get the method name and add suite prefix
        method = benchmark.get('Method', 'Unknown')
        full_name = f"{suite_prefix}/{method}"
        
        # Copy the benchmark with modified name
        modified = benchmark.copy()
        modified['Method'] = full_name
        modified['FullName'] = f"Abies.Benchmarks.{full_name}"
        
        benchmarks.append(modified)
    
    return benchmarks


def extract_allocation_metrics(data: dict, suite_prefix: str) -> list[dict]:
    """Extract allocation metrics in customSmallerIsBetter format."""
    results = []
    for benchmark in data.get('Benchmarks', []):
        method = benchmark.get('Method', 'Unknown')
        full_name = f"{suite_prefix}/{method}"
        
        memory = benchmark.get('Memory', {})
        bytes_allocated = memory.get('BytesAllocatedPerOperation', 0)
        
        # Also extract GC info for extra context
        gen0 = memory.get('Gen0Collections', 0)
        gen1 = memory.get('Gen1Collections', 0)
        gen2 = memory.get('Gen2Collections', 0)
        
        extra_parts = []
        if gen0 > 0:
            extra_parts.append(f"Gen0: {gen0:.4f}")
        if gen1 > 0:
            extra_parts.append(f"Gen1: {gen1:.4f}")
        if gen2 > 0:
            extra_parts.append(f"Gen2: {gen2:.4f}")
        
        result = {
            "name": full_name,
            "value": bytes_allocated,
            "unit": "bytes",
        }
        
        if extra_parts:
            result["extra"] = ", ".join(extra_parts)
        
        results.append(result)
    
    return results


def merge_benchmarkdotnet_format(all_benchmarks: list[dict], template: dict) -> dict:
    """Create merged BenchmarkDotNet format file."""
    merged = {
        "Title": "Abies Rendering Engine Benchmarks",
        "HostEnvironmentInfo": template.get("HostEnvironmentInfo", {}),
        "Benchmarks": all_benchmarks,
    }
    return merged


def main():
    if len(sys.argv) != 2:
        print(f"Usage: {sys.argv[0]} <benchmark-results-dir>")
        sys.exit(1)
    
    base_dir = Path(sys.argv[1])
    if not base_dir.exists():
        print(f"Error: Directory not found: {base_dir}")
        sys.exit(1)
    
    # Find benchmark files
    files = find_benchmark_files(base_dir)
    if not files:
        print(f"Error: No benchmark files found in {base_dir}")
        sys.exit(1)
    
    print(f"Found benchmark files:")
    for suite, path in files.items():
        print(f"  {suite}: {path}")
    
    # Create output directory
    output_dir = base_dir / 'merged'
    output_dir.mkdir(exist_ok=True)
    
    # Collect all metrics
    all_throughput = []
    all_allocations = []
    template_data = None
    
    suite_prefixes = {
        'diffing': 'Diffing',
        'rendering': 'Rendering',
        'handlers': 'Handlers',
    }
    
    for suite, path in files.items():
        data = load_benchmark_data(path)
        prefix = suite_prefixes.get(suite, suite.title())
        
        # Save template for merged output
        if template_data is None:
            template_data = data
        
        # Extract metrics
        throughput = extract_throughput_metrics(data, prefix)
        allocations = extract_allocation_metrics(data, prefix)
        
        all_throughput.extend(throughput)
        all_allocations.extend(allocations)
        
        print(f"  Extracted {len(throughput)} throughput and {len(allocations)} allocation metrics from {suite}")
    
    # Write merged throughput (BenchmarkDotNet format)
    throughput_file = output_dir / 'throughput.json'
    merged_throughput = merge_benchmarkdotnet_format(all_throughput, template_data or {})
    with open(throughput_file, 'w') as f:
        json.dump(merged_throughput, f, indent=2)
    print(f"\nWrote {len(all_throughput)} throughput metrics to {throughput_file}")
    
    # Write merged allocations (customSmallerIsBetter format)
    allocations_file = output_dir / 'allocations.json'
    with open(allocations_file, 'w') as f:
        json.dump(all_allocations, f, indent=2)
    print(f"Wrote {len(all_allocations)} allocation metrics to {allocations_file}")
    
    # Print summary
    print("\n" + "="*60)
    print("BENCHMARK SUMMARY")
    print("="*60)
    
    print("\nThroughput Metrics (ops/sec, higher is better):")
    for b in all_throughput[:5]:  # Show first 5
        stats = b.get('Statistics', {})
        mean = stats.get('Mean', 0) / 1_000_000  # Convert ns to ms
        print(f"  {b['Method']}: {mean:.3f} ms/op")
    if len(all_throughput) > 5:
        print(f"  ... and {len(all_throughput) - 5} more")
    
    print("\nAllocation Metrics (bytes, lower is better):")
    for a in all_allocations[:5]:  # Show first 5
        print(f"  {a['name']}: {a['value']:,} bytes")
    if len(all_allocations) > 5:
        print(f"  ... and {len(all_allocations) - 5} more")


if __name__ == '__main__':
    main()
