#!/usr/bin/env python3
"""
Extract memory allocation metrics from BenchmarkDotNet JSON output.

This script reads the full BenchmarkDotNet JSON report and generates a custom
JSON file compatible with github-action-benchmark's customSmallerIsBetter format.

Usage:
    python extract-allocations.py <input.json> <output.json>
"""

import json
import sys
from pathlib import Path


def extract_allocations(input_path: str, output_path: str) -> None:
    """Extract BytesAllocatedPerOperation from BenchmarkDotNet JSON."""
    with open(input_path, 'r') as f:
        data = json.load(f)

    results = []
    for benchmark in data.get('Benchmarks', []):
        name = benchmark.get('FullName', benchmark.get('Method', 'Unknown'))
        # Simplify name: remove namespace prefix
        if '.' in name:
            name = name.split('.')[-1]
        
        memory = benchmark.get('Memory', {})
        bytes_allocated = memory.get('BytesAllocatedPerOperation', 0)
        
        # Also extract GC info for extra context
        gen0 = memory.get('Gen0Collections', 0)
        gen1 = memory.get('Gen1Collections', 0)
        gen2 = memory.get('Gen2Collections', 0)
        
        extra_parts = []
        if gen0 > 0:
            extra_parts.append(f"Gen0: {gen0}")
        if gen1 > 0:
            extra_parts.append(f"Gen1: {gen1}")
        if gen2 > 0:
            extra_parts.append(f"Gen2: {gen2}")
        
        result = {
            "name": name,
            "value": bytes_allocated,
            "unit": "bytes",
        }
        
        if extra_parts:
            result["extra"] = ", ".join(extra_parts)
        
        results.append(result)
    
    with open(output_path, 'w') as f:
        json.dump(results, f, indent=2)
    
    print(f"Extracted {len(results)} allocation metrics to {output_path}")
    for r in results:
        print(f"  {r['name']}: {r['value']} {r['unit']}")


if __name__ == '__main__':
    if len(sys.argv) != 3:
        print(f"Usage: {sys.argv[0]} <input.json> <output.json>")
        sys.exit(1)
    
    extract_allocations(sys.argv[1], sys.argv[2])
